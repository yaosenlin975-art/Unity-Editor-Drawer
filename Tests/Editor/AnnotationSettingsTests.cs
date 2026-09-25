using Lin.Editor.Annotation;
using Lin.Editor.Annotation.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lin.Editor.Annotation.Tests
{
    /// <summary>
    /// 设置 SO 的默认值与迁移。只走 CreateInstance，绝不碰 AnnotationSettings.Instance——
    /// 那会在宿主工程的 ProjectSettings/ 下真的落文件，测试不该有磁盘副作用。
    /// </summary>
    public class AnnotationSettingsTests
    {
        [Test]
        public void S1_NewInstanceCarriesDocumentedDefaults()
        {
            var s = ScriptableObject.CreateInstance<AnnotationSettings>();

            Assert.AreEqual(14, s.assetSummaryTitleSize);
            Assert.AreEqual(EClickType.单击, s.assetSummaryEditWay);
            Assert.AreEqual(14, s.sceneObjectDescriptionTitleSize);
            Assert.AreEqual(EClickType.单击, s.sceneObjectDescriptionEditWay);
            // 门面原默认是字符串 "#808080"，SO 存 Color；断言走用户可见的 HTML 串，不比浮点位
            Assert.AreEqual("808080", ColorUtility.ToHtmlStringRGB(s.scriptDescriptionColor));
            Assert.IsFalse(s.scriptDescriptionBold);
            Assert.IsFalse(s.scriptDescriptionItalic);
            Assert.AreEqual(100, s.scriptDescriptionScanMaxLines);
            Assert.AreEqual("Description: \nDescription：\n功能说明: ", s.descriptionFiltersText);
            Assert.AreEqual(1, s.settingsVersion);
        }

        [Test]
        public void S2_MigrateRestoresDefaultsForFieldsMissingInOldFile()
        {
            // 老 .asset 缺字段时反序列化填的是 0/false/空，不是 C# 初始化值（ADR-001 第 4 条）
            var s = ScriptableObject.CreateInstance<AnnotationSettings>();
            s.settingsVersion = 0;
            s.assetSummaryTitleSize = 0;
            s.assetSummaryEditWay = default;
            s.sceneObjectDescriptionTitleSize = 0;
            s.scriptDescriptionScanMaxLines = 0;
            s.descriptionFiltersText = string.Empty;

            s.Migrate();

            Assert.AreEqual(1, s.settingsVersion);
            Assert.AreEqual(14, s.assetSummaryTitleSize);
            Assert.AreEqual(EClickType.单击, s.assetSummaryEditWay);
            Assert.AreEqual(14, s.sceneObjectDescriptionTitleSize);
            Assert.AreEqual(100, s.scriptDescriptionScanMaxLines);
            Assert.AreEqual("Description: \nDescription：\n功能说明: ", s.descriptionFiltersText);
        }
    }
}
