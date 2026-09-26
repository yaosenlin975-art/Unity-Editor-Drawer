using System.Collections.Generic;
using Lin.Editor.Annotation.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lin.Editor.Annotation.Tests
{
    /// <summary>
    /// 导入策略字段并入 AnnotationSettings 后的默认值与分级迁移。
    /// 与 AnnotationSettingsTests（注释设置那一份）分开：这里只锁被并进来的那十个字段与它带出的第二级迁移。
    /// 同 AnnotationSettingsTests 一样只走 CreateInstance，绝不碰 AnnotationSettings.Instance——
    /// 那会在宿主工程的 ProjectSettings/ 下真的落文件。
    /// </summary>
    public class AssetImportSettingsTests
    {
        // 对方 v1 的 Migrate() 是"整份回默认"。给 SO 加新字段时必须分级，
        // 否则老工程里用户真调过的 9 项注释设置会被导入策略的升级一起抹掉。
        [Test]
        public void T_M2_V1UserSettingsSurviveV2Migrate()
        {
            var s = ScriptableObject.CreateInstance<AnnotationSettings>();
            s.settingsVersion = 1;              // 对方已发布的 v1，带真值
            s.assetSummaryTitleSize = 22;
            s.scriptDescriptionScanMaxLines = 7;
            s.descriptionFiltersText = "自定义";

            s.Migrate();

            Assert.AreEqual(22, s.assetSummaryTitleSize, "v1 已有的值必须原样留着");
            Assert.AreEqual(7, s.scriptDescriptionScanMaxLines);
            Assert.AreEqual("自定义", s.descriptionFiltersText);
            Assert.AreEqual(2, s.settingsVersion);
            Assert.IsFalse(s.enabled, "新增字段拿自己的默认");
            Assert.AreEqual(3, s.overriddenPlatforms.Length);
        }

        /// <summary>十个新字段的默认值逐字段等于被删包里的那一份（ADR-003/ADR-006 的取值）。</summary>
        [Test]
        public void T_M2_MergedImportFieldsCarryDeletedPackageDefaults()
        {
            var s = ScriptableObject.CreateInstance<AnnotationSettings>();

            Assert.IsFalse(s.enabled, "装包即重写宿主 .meta 是侵略行为，必须默认关（ADR-003）");
            Assert.IsFalse(s.videoEnabled, "转码可达数小时，必须默认关（W6）");
            Assert.AreEqual(new[] { "Standalone", "Android", "iPhone" }, s.overriddenPlatforms);
            Assert.AreEqual(new[] { "WebGL", "WeixinMiniGame" }, s.followBuildDefaultPlatforms);
            Assert.AreEqual(2048, s.globalMaxTextureSize);
            Assert.AreEqual("Standalone", s.pcPlatform);
            Assert.AreEqual(new[] { "BGM", "背景" }, s.bgmKeywords);
            Assert.AreEqual(new[] { "Voice" }, s.voiceKeywords);
            Assert.AreEqual("_Loop", s.loopSuffix);
            Assert.AreEqual(1, s.importPolicyVersion);

            // 注释设置那九项一个字没被新字段带跑（S1 的另一面：这里只断言新字段没有污染旧默认值表）
            Assert.AreEqual(14, s.assetSummaryTitleSize);
            Assert.AreEqual(100, s.scriptDescriptionScanMaxLines);
        }

        /// <summary>真实的 v1 老文件：缺的十个字段反序列化填 0/false/null，补默认而不是整份回默认。</summary>
        [Test]
        public void T_M2_V1MigrateFillsOnlyEmptyImportFields()
        {
            var s = ScriptableObject.CreateInstance<AnnotationSettings>();
            s.settingsVersion = 1;
            s.sceneObjectDescriptionTitleSize = 9;    // 用户真调过的值，默认是 14
            s.globalMaxTextureSize = 0;
            s.pcPlatform = string.Empty;
            s.loopSuffix = null;
            s.overriddenPlatforms = null;
            s.followBuildDefaultPlatforms = new string[0];
            s.bgmKeywords = null;
            s.voiceKeywords = null;
            s.importPolicyVersion = 0;

            s.Migrate();

            Assert.AreEqual(9, s.sceneObjectDescriptionTitleSize, "老字段一律不动");
            Assert.AreEqual(2, s.settingsVersion);
            Assert.AreEqual(2048, s.globalMaxTextureSize);
            Assert.AreEqual("Standalone", s.pcPlatform);
            Assert.AreEqual("_Loop", s.loopSuffix);
            Assert.AreEqual(3, s.overriddenPlatforms.Length);
            Assert.AreEqual(2, s.followBuildDefaultPlatforms.Length);
            Assert.AreEqual(2, s.bgmKeywords.Length);
            Assert.AreEqual(1, s.voiceKeywords.Length);
            Assert.AreEqual(1, s.importPolicyVersion);
        }

        /// <summary>
        /// v0（早于 settingsVersion 字段）仍走对方原来的整份回默认。
        /// Migrate() 一次调用只升一级（S2 锁的就是这条），收敛到 CurrentVersion 是 Instance 里那个 while 循环的事；
        /// 这里复刻那个循环来锁"一次访问即补满导入字段"，因为 Instance 会在宿主工程 ProjectSettings/ 下真落文件，测试不能碰。
        /// </summary>
        [Test]
        public void T_M2_V0FileStillUsesOriginalWholesaleReset()
        {
            var s = ScriptableObject.CreateInstance<AnnotationSettings>();
            s.settingsVersion = 0;
            s.assetSummaryTitleSize = 0;
            s.sceneObjectDescriptionTitleSize = 0;
            s.scriptDescriptionScanMaxLines = 0;
            s.descriptionFiltersText = string.Empty;

            s.Migrate();

            Assert.AreEqual(1, s.settingsVersion, "单次 Migrate() 只升一级，不越级");
            Assert.AreEqual(14, s.assetSummaryTitleSize);
            Assert.AreEqual(14, s.sceneObjectDescriptionTitleSize);
            Assert.AreEqual(100, s.scriptDescriptionScanMaxLines);
            Assert.AreEqual("Description: \nDescription：\n功能说明: ", s.descriptionFiltersText);

            // Instance 的收敛循环：读到 v0 文件时一次访问就逐级调到位，落盘的不是"版本号 1 + 导入字段全空"的半成品
            while (s.settingsVersion < AnnotationSettings.CurrentVersion)
                s.Migrate();

            Assert.AreEqual(2, s.settingsVersion);
            Assert.AreEqual(3, s.overriddenPlatforms.Length, "同一次访问内导入字段就得补满");
            Assert.AreEqual(2048, s.globalMaxTextureSize);
        }

        /// <summary>平台两组互斥且齐全（W1：Web/小游戏不得进覆盖组），策略类只认这三个入口。</summary>
        [Test]
        public void T_M2_PlatformGroupsStayExclusiveAndTotal()
        {
            var s = ScriptableObject.CreateInstance<AnnotationSettings>();

            Assert.IsTrue(s.IsOverridden("Standalone"));
            Assert.IsTrue(s.IsWebPlatform("WebGL"));
            Assert.IsTrue(s.IsWebPlatform("WeixinMiniGame"));
            Assert.IsFalse(s.IsOverridden("WebGL"), "Web 平台不得进覆盖组（W1）");
            Assert.IsFalse(s.IsWebPlatform("Android"));
            Assert.IsFalse(s.IsOverridden(null));
            Assert.IsFalse(s.IsWebPlatform(string.Empty));

            var all = new List<string>(s.AllPlatforms);
            Assert.AreEqual(5, all.Count);
            Assert.IsFalse(all.Exists(p => s.IsOverridden(p) && s.IsWebPlatform(p)), "两组不得重叠");
        }
    }
}
