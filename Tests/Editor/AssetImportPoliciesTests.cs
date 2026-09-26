using Lin.Editor.AssetImport;
using Lin.Editor.Annotation.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lin.Editor.Annotation.Tests
{
    /// <summary>
    /// 三个纯策略类的用例合并（老计划 Task 3 / 4 / 5 的 T7–T11）。
    /// 这三张策略表就是任务本身：BC7/BC5/BC6H/ASTC 档位、4096/2048、quality 0.8/0.6/0.5、
    /// Web 组三类一律 CompressedInMemory、原生 SFX 用 PCM、只在已有 clip 数 ≤ 0 时播种、循环只按序数后缀 —— 逐档抄 Learn 的真值，
    /// 一个数值都不许在转录时改动（"更漂亮"的重构改错一个数字就是缺陷）。
    /// 形参类型由老计划的 AssetImportSettings 换成 M2 并入后的 AnnotationSettings；
    /// 平台分组判据（IsOverridden/IsWebPlatform/AllPlatforms）也在 AnnotationSettings 上，这里只消费、不重实现。
    /// </summary>
    public class AssetImportPoliciesTests
    {
        // 只用 CreateInstance，绝不碰 AnnotationSettings.Instance —— 那会在宿主 ProjectSettings/ 下真落文件。
        private static AnnotationSettings S() => ScriptableObject.CreateInstance<AnnotationSettings>();

        #region - Task 3: PlatformTextureFormat -

        [Test]
        public void T7_TableMatchesLearnPolicyPerUsageAndPlatform()
        {
            var s = S();

            Assert.AreEqual((TextureImporterFormat.BC7, 2048), PlatformTextureFormat.Get(TextureUsage.UI, "Standalone", s));
            Assert.AreEqual((TextureImporterFormat.ASTC_4x4, 2048), PlatformTextureFormat.Get(TextureUsage.UI, "Android", s));
            Assert.AreEqual((TextureImporterFormat.ASTC_4x4, 2048), PlatformTextureFormat.Get(TextureUsage.UI, "iPhone", s));

            Assert.AreEqual((TextureImporterFormat.BC5, 2048), PlatformTextureFormat.Get(TextureUsage.NormalMap, "Standalone", s));
            Assert.AreEqual((TextureImporterFormat.ASTC_4x4, 2048), PlatformTextureFormat.Get(TextureUsage.NormalMap, "iPhone", s));

            Assert.AreEqual((TextureImporterFormat.BC6H, 4096), PlatformTextureFormat.Get(TextureUsage.Lightmap, "Standalone", s));
            Assert.AreEqual((TextureImporterFormat.ASTC_8x8, 2048), PlatformTextureFormat.Get(TextureUsage.Lightmap, "Android", s));

            Assert.AreEqual((TextureImporterFormat.BC7, 4096), PlatformTextureFormat.Get(TextureUsage.Default, "Standalone", s));
            Assert.AreEqual((TextureImporterFormat.ASTC_4x4, 2048), PlatformTextureFormat.Get(TextureUsage.Default, "Android", s, hasAlpha: true));
            Assert.AreEqual((TextureImporterFormat.ASTC_6x6, 2048), PlatformTextureFormat.Get(TextureUsage.Default, "Android", s, hasAlpha: false));

            Assert.AreEqual((TextureImporterFormat.BC7, 2048), PlatformTextureFormat.Get(TextureUsage.Atlas, "Standalone", s));
            Assert.AreEqual((TextureImporterFormat.ASTC_6x6, 2048), PlatformTextureFormat.Get(TextureUsage.Atlas, "iPhone", s));
        }

        [Test]
        public void T8_WebPlatformsAreNeverOverridden()
        {
            var s = S();

            Assert.IsFalse(s.IsOverridden("WebGL"));
            Assert.IsFalse(s.IsOverridden("WeixinMiniGame"));

            // 万一有人把 Web 平台塞进覆盖组，Get 仍要给答案——但覆盖与否由 IsOverridden 决定，
            // 这条断言锁的是"判据只有 IsOverridden 一个入口"，不锁 Get 的行为。
            Assert.IsTrue(s.IsWebPlatform("WebGL"));
        }

        [Test]
        public void T8b_PcPlatformNameDrivesFormatBranch()
        {
            var s = S();
            s.pcPlatform = "iPhone";

            Assert.AreEqual(TextureImporterFormat.BC7, PlatformTextureFormat.Get(TextureUsage.UI, "iPhone", s).format,
                "把 pcPlatform 改成 iPhone 后，iPhone 就该走 PC 档（BC7），证明分支只看这个字段");
        }

        #endregion

        #region - Task 4: AudioImportPolicy -

        [Test]
        public void T9a_Classify_ByPathKeyword()
        {
            var s = S();
            Assert.AreEqual(AudioRole.Bgm, AudioImportPolicy.Classify("Assets/Audio/BGM/battle.mp3", s));
            Assert.AreEqual(AudioRole.Bgm, AudioImportPolicy.Classify("Assets/Audio/背景/main.mp3", s), "中文关键字与大小写都要吃");
            Assert.AreEqual(AudioRole.Bgm, AudioImportPolicy.Classify("Assets/Audio/bgm/city.mp3", s), "大小写不敏感");
            Assert.AreEqual(AudioRole.Voice, AudioImportPolicy.Classify("Assets/Audio/Voice/npc01.wav", s));
            Assert.AreEqual(AudioRole.Sfx, AudioImportPolicy.Classify("Assets/Audio/Footstep/stone.wav", s));
            Assert.AreEqual(AudioRole.Bgm, AudioImportPolicy.Classify("Assets/BGM/Voice/x.mp3", s), "BGM 优先于 Voice，与 Learn 的判定顺序一致");
        }

        [Test]
        public void T9b_NativeGroup_FollowsDocs()
        {
            var s = S();

            var bgm = AudioImportPolicy.Resolve(AudioRole.Bgm, "Standalone", s);
            Assert.AreEqual(AudioClipLoadType.Streaming, bgm.loadType);
            Assert.AreEqual(AudioCompressionFormat.Vorbis, bgm.format);
            Assert.AreEqual(0.8f, bgm.quality, 0.001f);

            var voice = AudioImportPolicy.Resolve(AudioRole.Voice, "Android", s);
            Assert.AreEqual(AudioClipLoadType.CompressedInMemory, voice.loadType);
            Assert.AreEqual(0.6f, voice.quality, 0.001f);

            // W4：文档 "PCM ... is best for short sound effects"
            var sfx = AudioImportPolicy.Resolve(AudioRole.Sfx, "iPhone", s);
            Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, sfx.loadType);
            Assert.AreEqual(AudioCompressionFormat.PCM, sfx.format);
        }

        [Test]
        public void T9c_WebGroupNeverStreamsNeverDecompressesOnLoad()
        {
            var s = S();
            foreach (var platform in new[] { "WebGL", "WeixinMiniGame" })
            {
                foreach (var role in new[] { AudioRole.Bgm, AudioRole.Voice, AudioRole.Sfx })
                {
                    var d = AudioImportPolicy.Resolve(role, platform, s);
                    Assert.AreEqual(AudioClipLoadType.CompressedInMemory, d.loadType,
                        $"{platform}/{role} 必须是 CompressedInMemory：WebGL 只列了两种合法 loadType，" +
                        "而 Vorbis 载入解压吃 10 倍内存（W2）");
                    Assert.AreNotEqual(AudioClipLoadType.Streaming, d.loadType);
                    Assert.AreNotEqual(AudioClipLoadType.DecompressOnLoad, d.loadType);
                    Assert.AreEqual(AudioCompressionFormat.Vorbis, d.format, "Web 组包体优先，不上 PCM（W4）");
                }
            }
        }

        [Test]
        public void T9d_UnknownPlatformTreatedAsNative()
        {
            var s = S();
            var d = AudioImportPolicy.Resolve(AudioRole.Sfx, "Switch", s);
            Assert.AreEqual(AudioCompressionFormat.PCM, d.format, "不在两组里的平台按原生处理，别默默降级");
        }

        #endregion

        #region - Task 5: ModelImportPolicy -

        [Test]
        public void T10_SeedOnlyWhenClipAnimationsEmpty()
        {
            Assert.IsTrue(ModelImportPolicy.ShouldSeedClips(0), "首次导入 clipAnimations 恒为空，此时才该播种");
            Assert.IsTrue(ModelImportPolicy.ShouldSeedClips(-1), "null 数组按空处理");
            Assert.IsFalse(ModelImportPolicy.ShouldSeedClips(1), "已有 clip 配置说明美术动过手，覆写会抹掉定制（W3）");
            Assert.IsFalse(ModelImportPolicy.ShouldSeedClips(12));
        }

        [Test]
        public void T11_LoopBySuffixOnly()
        {
            var s = S();

            Assert.IsTrue(ModelImportPolicy.ShouldLoop("Run_Loop", s));
            Assert.IsTrue(ModelImportPolicy.ShouldLoop("_Loop", s), "整名即后缀也算");
            Assert.IsFalse(ModelImportPolicy.ShouldLoop("Idle", s));
            Assert.IsFalse(ModelImportPolicy.ShouldLoop("loop_Run", s), "结尾才算，别把前缀误判");
            Assert.IsFalse(ModelImportPolicy.ShouldLoop("Run_loop", s), "区分大小写：Unity 的 clip 名是序数比较");
            Assert.IsFalse(ModelImportPolicy.ShouldLoop(null, s));
            Assert.IsFalse(ModelImportPolicy.ShouldLoop("Run_Loop", EmptySuffix(s)), "后缀为空时不匹配任何 clip，别恒真");
        }

        private static AnnotationSettings EmptySuffix(AnnotationSettings s)
        {
            s.loopSuffix = string.Empty;
            return s;
        }

        #endregion
    }
}
