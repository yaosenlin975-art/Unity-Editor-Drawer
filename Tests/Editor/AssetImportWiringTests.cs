using System.Reflection;
using Lin.Editor.AssetImport;
using Lin.Editor.Annotation.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Lin.Editor.Annotation.Tests
{
    /// <summary>
    /// M4 接线层的四条可单测判据。策略落地本身（AssetModifier 写 importer）不吃单测——它是 importer API 的壳，
    /// 真判据在 M4 的 batch 探针里（Logs/m4-e2e.txt）。这里只钉四件"编译期看不出来、跑一次导入又太贵"的事：
    /// 1) OnPreprocess* 是按名字反射调用的魔法方法（2021.3/2022.3 的 UnityEditor 程序集里搜不到这些方法名），
    ///    写错名字照样编译通过、只是永不被调用 —— 名字拼写由 W1 钉，"真的被调到了"由探针钉。
    /// 2) AssetModifier 每个入口的 null 守卫：回调侧随时可能给 null，炸了就是一次导入失败。
    /// 3) LogTextImporter 的 [ScriptedImporter] 注册还在（删掉属性 = .log 悄悄退回默认导入）。
    /// 4) 音频两条承载通道各自的输入值（W4，见该用例注释：哪一份值交给哪个 API 只能由探针证）。
    /// </summary>
    public class AssetImportWiringTests
    {
        [Test]
        public void W1_PostprocessorDeclaresTheFourMagicCallbacksByExactName()
        {
            // 逐个点名：任何一个拼错，Unity 不会报错、只会永远不调它
            AssertCallback("OnPreprocessTexture");
            AssertCallback("OnPreprocessAudio");
            AssertCallback("OnPreprocessModel");
            AssertCallback("OnPreprocessAnimation");
        }

        private static void AssertCallback(string name)
        {
            var m = typeof(MediaAssetPostprocessor).GetMethod(
                name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null, System.Type.EmptyTypes, null);

            Assert.IsNotNull(m, name + " 必须以『无参实例方法』的确切名字声明 —— 基类没有它，反射按名字调");
            Assert.AreEqual(typeof(void), m.ReturnType, name + " 的返回类型必须是 void，否则反射不调用");
        }

        [Test]
        public void W2_ApplyEntryPointsTolerateNullImporter()
        {
            AssetModifier.ApplyTexture(null);
            AssetModifier.ApplyAudio(null);
            AssetModifier.ApplyModelBase(null);
            AssetModifier.ApplyClips(null);
            AssetModifier.ApplySpriteAtlas(null);
            AssetModifier.ApplyVideo(null);

            Assert.AreEqual((0, 0), AssetModifier.GetTextureImporterSize(null));
        }

        [Test]
        public void W3_LogTextImporterIsRegisteredAsAScriptedImporter()
        {
            var attrs = typeof(LogTextImporter).GetCustomAttributes(typeof(ScriptedImporterAttribute), false);
            Assert.AreEqual(1, attrs.Length, "只该有一条 [ScriptedImporter]，两条会让扩展名归属不确定");
            Assert.IsTrue(typeof(ScriptedImporter).IsAssignableFrom(typeof(LogTextImporter)),
                "LogTextImporter 必须直接是 ScriptedImporter，否则不会进导入管线");
        }

        /// <summary>
        /// 音频的分组分工（fix round 1）：<see cref="AssetModifier.ApplyAudio"/> 把覆盖组交给
        /// <c>SetOverrideSampleSettings</c>、把跟随构建组交给 <c>defaultSampleSettings</c>，
        /// 因为实测（audio-probe.md Q1/Q2）只有前者落 <c>platformSettingOverrides</c> 的 1/4/7 三行，
        /// WebGL 与 WeixinMiniGame 永不落行、只能通过后者的 <c>defaultSettings:</c> 块传过去。
        /// 这条用例锁的是"交给哪个 API 的那一份决策是什么值"，也就是两条通道各自的输入；
        /// 它锁不到"值确实走了那条 API"—— 本进程内造不出带 assetPath 的 AudioImporter，
        /// 而 SetOverrideSampleSettings 对 WebGL 是静默 no-op（不抛、不告警、读回还是默认），
        /// 所以任何"在内存 importer 上跑一遍再读回"的写法都只是在断言 Unity 自己的行为，改了实现也不会红。
        /// 承载通道这一层的判据在探针（Logs/audio-fix-probe-red.txt / -green.txt 的 .meta 原文）。
        /// </summary>
        [Test]
        public void W4_AudioDecisionsSplitByPlatformGroupAcrossTheTwoImporterChannels()
        {
            // 只用 CreateInstance：AnnotationSettings.Instance 会在宿主 ProjectSettings/ 下真落文件
            var s = ScriptableObject.CreateInstance<AnnotationSettings>();

            // 覆盖通道：三行都得是原生档位（Bgm 流送 / Voice 压内存 / Sfx 解压加载 + PCM）
            foreach (var platform in s.overriddenPlatforms)
            {
                var bgm = AudioImportPolicy.Resolve(AudioRole.Bgm, platform, s);
                Assert.AreEqual(AudioClipLoadType.Streaming, bgm.loadType, $"{platform} 的 BGM 走覆盖行，可以流送");
                Assert.AreEqual(AudioCompressionFormat.Vorbis, bgm.format);
                Assert.AreEqual(0.8f, bgm.quality, 0.001f);

                var voice = AudioImportPolicy.Resolve(AudioRole.Voice, platform, s);
                Assert.AreEqual(AudioClipLoadType.CompressedInMemory, voice.loadType);
                Assert.AreEqual(0.6f, voice.quality, 0.001f);

                var sfx = AudioImportPolicy.Resolve(AudioRole.Sfx, platform, s);
                Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, sfx.loadType, "W4：原生短音效 PCM 解压加载");
                Assert.AreEqual(AudioCompressionFormat.PCM, sfx.format);
                Assert.AreEqual(0.5f, sfx.quality, 0.001f);
            }

            // 跟随构建组：defaultSampleSettings 那份就是 Web 组的决策，三类一律压内存，quality 随角色
            foreach (var platform in s.followBuildDefaultPlatforms)
            {
                Assert.AreEqual(AudioClipLoadType.CompressedInMemory,
                    AudioImportPolicy.Resolve(AudioRole.Bgm, platform, s).loadType, $"{platform}/Bgm 是 W2 禁 DecompressOnLoad 的那一条");
                Assert.AreEqual(AudioClipLoadType.CompressedInMemory,
                    AudioImportPolicy.Resolve(AudioRole.Voice, platform, s).loadType);
                var webSfx = AudioImportPolicy.Resolve(AudioRole.Sfx, platform, s);
                Assert.AreEqual(AudioClipLoadType.CompressedInMemory, webSfx.loadType);
                Assert.AreEqual(AudioCompressionFormat.Vorbis, webSfx.format,
                    "写进 defaultSettings 的字面格式；WebGL 读回仍是引擎钉的 AAC(7)，所以这里不承诺 Web 侧格式");
                Assert.AreEqual(0.5f, webSfx.quality, 0.001f);
            }

            // 为什么非得分两条 API：可用性判据挡不住 WebGL —— 它在 BuildTargetGroup 里是定义的、模块也装着，
            // 音频侧却照样不落行。所以"跳过不可用平台"那段守卫不是分组的替代品，分组才是。
            Assert.IsTrue(PlatformTextureFormat.IsPlatformAvailable("WebGL"),
                "WebGL 可用但仍不落 platformSettingOverrides 行，可用性不是筛选条件");
            Assert.IsFalse(PlatformTextureFormat.IsPlatformAvailable("WeixinMiniGame"),
                "主线 Unity 2021.3 无 WeixinMiniGame 这个枚举成员（实测），它同样只能走 defaultSettings");
        }
    }
}
