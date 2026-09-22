using Lin.Editor.Annotation.Asset;
using Lin.Editor.Annotation.Toolbar.Element;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lin.Editor.Annotation.Settings
{
    /// <summary>
    /// Project Settings 里的注释配置页，分资源注释 / 场景物体注释 / 脚本注释三段。
    /// 原先这些值住在项目私有的 EditorSettings_SO 上，整页搬过来会把 Learn 的框架配置一起带进包，
    /// 所以只保留与注释有关的三段。
    /// </summary>
    internal static class EditorAnnotationSettingsProvider
    {
        public const string PagePath = "Project/Lin Editor Annotation";

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider(PagePath, SettingsScope.Project)
            {
                label = "Lin Editor Annotation",
                keywords = new HashSet<string> { "Annotation", "Hierarchy", "Toolbar", "注释", "Description", "Summary", "脚本" },
                guiHandler = _ => DrawGUI()
            };
        }

        [MenuItem("Tools/Lin Editor Annotation/注释设置")]
        private static void OpenFromMenu() => SettingsService.OpenProjectSettings(PagePath);

        /// <summary>包自带的主工具栏入口：一个按钮直达本页。</summary>
        [ToolbarButton(EAlign.Right, EVisibleMode.Editor, "注释", "打开注释设置")]
        private static void OpenFromToolbar() => SettingsService.OpenProjectSettings(PagePath);

        private static void DrawGUI()
        {
            bool changed = false;

            Section("资源注释（Project 窗口）");
            changed |= IntField(nameof(EditorAnnotationSettings.AssetSummaryTitleSize), "注释字体大小",
                EditorAnnotationSettings.AssetSummaryTitleSize, v => EditorAnnotationSettings.AssetSummaryTitleSize = v);
            changed |= EnumField(nameof(EditorAnnotationSettings.AssetSummaryEditWay), "点击注释修改",
                EditorAnnotationSettings.AssetSummaryEditWay, v => EditorAnnotationSettings.AssetSummaryEditWay = v);

            Section("场景物体注释（Hierarchy 行内）");
            changed |= IntField(nameof(EditorAnnotationSettings.SceneObjectDescriptionTitleSize), "注释字体大小",
                EditorAnnotationSettings.SceneObjectDescriptionTitleSize, v => EditorAnnotationSettings.SceneObjectDescriptionTitleSize = v);
            changed |= EnumField(nameof(EditorAnnotationSettings.SceneObjectDescriptionEditWay), "点击注释修改",
                EditorAnnotationSettings.SceneObjectDescriptionEditWay, v => EditorAnnotationSettings.SceneObjectDescriptionEditWay = v);

            Section("脚本注释（取自 .cs 源码头部注释）");
            changed |= ColorField(nameof(EditorAnnotationSettings.ScriptDescriptionColor), "颜色",
                EditorAnnotationSettings.ScriptDescriptionColor, v => EditorAnnotationSettings.ScriptDescriptionColor = v);
            changed |= BoolField(nameof(EditorAnnotationSettings.ScriptDescriptionBold), "粗体",
                EditorAnnotationSettings.ScriptDescriptionBold, v => EditorAnnotationSettings.ScriptDescriptionBold = v);
            changed |= BoolField(nameof(EditorAnnotationSettings.ScriptDescriptionItalic), "斜体",
                EditorAnnotationSettings.ScriptDescriptionItalic, v => EditorAnnotationSettings.ScriptDescriptionItalic = v);

            // 一行一个标识；存进 EditorPrefs 时就是这段原始文本
            EditorGUILayout.LabelField(new GUIContent("描述标识", "在 .cs 文件里识别描述行的前缀，一行一个"));
            var edited = EditorGUILayout.TextArea(
                EditorAnnotationSettings.DescriptionFiltersText,
                EditorStyles.textArea,
                GUILayout.MinHeight(54f));
            if (edited != EditorAnnotationSettings.DescriptionFiltersText)
            {
                EditorAnnotationSettings.DescriptionFiltersText = edited;
                changed = true;
            }

            // Project 窗口的绘制结果带缓存，样式或标识变了要重扫；Hierarchy 每帧现读，不需要
            if (changed)
                AssetSummaryDrawer.Refresh();
        }

        private static void Section(string title)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private static bool IntField(string key, string label, int current, System.Action<int> apply)
        {
            var value = EditorGUILayout.IntField(new GUIContent(label, GetTooltip(key)), current);
            value = Mathf.Clamp(value, 8, 32);
            if (value == current) return false;
            apply(value);
            return true;
        }

        private static bool BoolField(string key, string label, bool current, System.Action<bool> apply)
        {
            var value = EditorGUILayout.Toggle(new GUIContent(label, GetTooltip(key)), current);
            if (value == current) return false;
            apply(value);
            return true;
        }

        private static bool ColorField(string key, string label, Color current, System.Action<Color> apply)
        {
            var value = EditorGUILayout.ColorField(new GUIContent(label, GetTooltip(key)), current);
            if (value == current) return false;
            apply(value);
            return true;
        }

        private static bool EnumField(string key, string label, EClickType current, System.Action<EClickType> apply)
        {
            var value = (EClickType)EditorGUILayout.EnumPopup(new GUIContent(label, GetTooltip(key)), current);
            if (value == current) return false;
            apply(value);
            return true;
        }

        private static string GetTooltip(string key) =>
            $"存储于 EditorPrefs：{EditorAnnotationSettings.PrefKey(key)}（本机本用户，不随工程进版本库）";
    }
}
