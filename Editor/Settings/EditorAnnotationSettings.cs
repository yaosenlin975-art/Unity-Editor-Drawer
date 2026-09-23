using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lin.Editor.Annotation.Settings
{
    /// <summary>
    /// 本包的用户级编辑器偏好。原本挂在项目私有 EditorSettings_SO 上的注释相关字段落在这里，
    /// 使包不需要在项目里创建或依赖任何配置资产。
    /// </summary>
    public static class EditorAnnotationSettings
    {
        /// <summary>包内资产的加载路径前缀，与目录名绑定。</summary>
        public const string PackagePath = "Packages/com.lin.editor-annotation";

        /// <summary>两个注释窗口共用的样式表（两份原文件逐字节相同，故合一份）。</summary>
        public const string WindowStyleSheetPath = PackagePath + "/Editor/UI/AnnotationWindow.uss";

        /// <summary>给使用方提供的 EditorPrefs key 前缀入口，避免与其它包撞 key。</summary>
        public static string PrefKey(string name) => "com.lin.editor-annotation/" + name;

        /// <summary>注释标题未指定颜色时的兜底值，沿用原 EditorConst 的取值。</summary>
        public static readonly Color DescriptionTitleDefaultColor = new Color(0.267f, 2 / 3f, 1, 1);

        private const int DefaultTitleSize = 14;

        internal static EAnnotationLanguage Language
        {
            get => EditorPrefs.GetInt(PrefKey(nameof(Language)), (int)EAnnotationLanguage.Chinese) == (int)EAnnotationLanguage.English
                ? EAnnotationLanguage.English
                : EAnnotationLanguage.Chinese;
            set => EditorPrefs.SetInt(PrefKey(nameof(Language)), (int)value);
        }

        // ----------------- Project 窗口资源注释 -----------------

        public static int AssetSummaryTitleSize
        {
            get => EditorPrefs.GetInt(PrefKey(nameof(AssetSummaryTitleSize)), DefaultTitleSize);
            set => EditorPrefs.SetInt(PrefKey(nameof(AssetSummaryTitleSize)), value);
        }

        public static EClickType AssetSummaryEditWay
        {
            get => (EClickType)EditorPrefs.GetInt(PrefKey(nameof(AssetSummaryEditWay)), (int)EClickType.单击);
            set => EditorPrefs.SetInt(PrefKey(nameof(AssetSummaryEditWay)), (int)value);
        }

        // ----------------- Hierarchy 场景物体注释 -----------------

        public static int SceneObjectDescriptionTitleSize
        {
            get => EditorPrefs.GetInt(PrefKey(nameof(SceneObjectDescriptionTitleSize)), DefaultTitleSize);
            set => EditorPrefs.SetInt(PrefKey(nameof(SceneObjectDescriptionTitleSize)), value);
        }

        public static EClickType SceneObjectDescriptionEditWay
        {
            get => (EClickType)EditorPrefs.GetInt(PrefKey(nameof(SceneObjectDescriptionEditWay)), (int)EClickType.单击);
            set => EditorPrefs.SetInt(PrefKey(nameof(SceneObjectDescriptionEditWay)), (int)value);
        }

        // ----------------- 脚本注释（从 .cs 源码头部注释里提取） -----------------

        public static Color ScriptDescriptionColor
        {
            get
            {
                // 默认取中灰：Alpha 不参与 ToHtmlStringRGB，深色/浅色主题下都可读
                if (!ColorUtility.TryParseHtmlString(
                        "#" + EditorPrefs.GetString(PrefKey(nameof(ScriptDescriptionColor)), "808080"), out var color))
                    return Color.gray;
                return color;
            }
            set => EditorPrefs.SetString(PrefKey(nameof(ScriptDescriptionColor)), ColorUtility.ToHtmlStringRGBA(value));
        }

        public static bool ScriptDescriptionBold
        {
            get => EditorPrefs.GetBool(PrefKey(nameof(ScriptDescriptionBold)), false);
            set => EditorPrefs.SetBool(PrefKey(nameof(ScriptDescriptionBold)), value);
        }

        public static bool ScriptDescriptionItalic
        {
            get => EditorPrefs.GetBool(PrefKey(nameof(ScriptDescriptionItalic)), false);
            set => EditorPrefs.SetBool(PrefKey(nameof(ScriptDescriptionItalic)), value);
        }

        /// <summary>描述标识的原始存储：换行分隔，一行一个。</summary>
        public static string DescriptionFiltersText
        {
            get => EditorPrefs.GetString(PrefKey(nameof(DescriptionFilters)), DefaultDescriptionFiltersText);
            set => EditorPrefs.SetString(PrefKey(nameof(DescriptionFilters)), value);
        }

        private const string DefaultDescriptionFiltersText = "Description: \nDescription：\n功能说明: ";

        /// <summary>
        /// 供绘制使用的标识列表。空行必须滤掉——空标识会让 Contains("") 恒真，把源码首行当成注释。
        /// </summary>
        public static List<string> DescriptionFilters
        {
            get
            {
                var result = new List<string>();
                foreach (var line in DescriptionFiltersText.Split('\n'))
                    if (!string.IsNullOrEmpty(line))
                        result.Add(line);
                return result;
            }
        }

        /// <summary>把共用样式表挂到窗口根节点上，两个注释窗口都用它。</summary>
        public static void ApplyWindowStyleSheet(UnityEngine.UIElements.VisualElement root)
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(WindowStyleSheetPath);
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);
        }
    }
}
