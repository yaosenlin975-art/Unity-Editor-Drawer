using Lin.Editor.Annotation.Asset;
using Lin.Editor.Toolbar.Element;
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
        public const string PagePath = "Project/Lin Editor Drawer";
        private static SettingsProvider provider;
        private static GUIStyle sizePreviewStyle;

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            provider = new SettingsProvider(PagePath, SettingsScope.Project)
            {
                label = EditorAnnotationLocalization.Text(EAnnotationText.SettingsPageLabel),
                keywords = new HashSet<string> { "Annotation", "Hierarchy", "Toolbar", "注释", "Description", "Summary", "脚本" },
                guiHandler = _ => DrawGUI()
            };
            EditorAnnotationLocalization.LanguageChanged -= OnLanguageChanged;
            EditorAnnotationLocalization.LanguageChanged += OnLanguageChanged;
            return provider;
        }

        [MenuItem("Lin/Editor Drawer - 编辑器绘制/Annotation Settings - 设置界面")]
        private static void OpenFromMenu() => OpenSettings();

        private static void OpenSettings() => SettingsService.OpenProjectSettings(PagePath);

        /// <summary>包自带的主工具栏入口：一个按钮直达本页。</summary>
        [ToolbarButton(EAlign.Right, EVisibleMode.Editor, "注释", "打开注释设置")]
        private static void OpenFromToolbar() => SettingsService.OpenProjectSettings(PagePath);

        private static void OnLanguageChanged()
        {
            if (provider != null)
                provider.label = EditorAnnotationLocalization.Text(EAnnotationText.SettingsPageLabel);
        }

        private static void DrawGUI()
        {
            bool changed = false;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(EditorAnnotationLocalization.Text(EAnnotationText.SwitchLanguage), GUILayout.Width(90)))
                EditorAnnotationLocalization.ToggleLanguage();
            GUILayout.EndHorizontal();

            Section(EditorAnnotationLocalization.Text(EAnnotationText.AssetSection));
            changed |= IntSliderField(nameof(EditorAnnotationSettings.AssetSummaryTitleSize), EditorAnnotationLocalization.Text(EAnnotationText.TitleSize),
                EditorAnnotationSettings.AssetSummaryTitleSize, EditorAnnotationLocalization.Text(EAnnotationText.SizePreview),
                v => EditorAnnotationSettings.AssetSummaryTitleSize = v);
            changed |= EnumField(nameof(EditorAnnotationSettings.AssetSummaryEditWay), EditorAnnotationLocalization.Text(EAnnotationText.EditOnClick),
                EditorAnnotationSettings.AssetSummaryEditWay, v => EditorAnnotationSettings.AssetSummaryEditWay = v);

            Section(EditorAnnotationLocalization.Text(EAnnotationText.SceneSection));
            changed |= IntSliderField(nameof(EditorAnnotationSettings.SceneObjectDescriptionTitleSize), EditorAnnotationLocalization.Text(EAnnotationText.TitleSize),
                EditorAnnotationSettings.SceneObjectDescriptionTitleSize, EditorAnnotationLocalization.Text(EAnnotationText.SizePreview),
                v => EditorAnnotationSettings.SceneObjectDescriptionTitleSize = v);
            changed |= EnumField(nameof(EditorAnnotationSettings.SceneObjectDescriptionEditWay), EditorAnnotationLocalization.Text(EAnnotationText.EditOnClick),
                EditorAnnotationSettings.SceneObjectDescriptionEditWay, v => EditorAnnotationSettings.SceneObjectDescriptionEditWay = v);

            Section(EditorAnnotationLocalization.Text(EAnnotationText.ScriptSection));
            changed |= ColorField(nameof(EditorAnnotationSettings.ScriptDescriptionColor), EditorAnnotationLocalization.Text(EAnnotationText.Color),
                EditorAnnotationSettings.ScriptDescriptionColor, v => EditorAnnotationSettings.ScriptDescriptionColor = v);
            changed |= BoolField(nameof(EditorAnnotationSettings.ScriptDescriptionBold), EditorAnnotationLocalization.Text(EAnnotationText.Bold),
                EditorAnnotationSettings.ScriptDescriptionBold, v => EditorAnnotationSettings.ScriptDescriptionBold = v);
            changed |= BoolField(nameof(EditorAnnotationSettings.ScriptDescriptionItalic), EditorAnnotationLocalization.Text(EAnnotationText.Italic),
                EditorAnnotationSettings.ScriptDescriptionItalic, v => EditorAnnotationSettings.ScriptDescriptionItalic = v);
            changed |= IntField(nameof(EditorAnnotationSettings.ScriptDescriptionScanMaxLines), EditorAnnotationLocalization.Text(EAnnotationText.ScanMaxLines),
                EditorAnnotationSettings.ScriptDescriptionScanMaxLines, v => EditorAnnotationSettings.ScriptDescriptionScanMaxLines = v);

            changed |= DescriptionMarkersList();

            // Project 窗口的绘制结果带缓存，样式或标识变了要重扫；Hierarchy 每帧现读，不需要
            if (changed)
                AssetSummaryDrawer.Refresh();
        }

        private static void Section(string title)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private static bool IntSliderField(string key, string label, int current, string preview, System.Action<int> apply)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(label, GetTooltip(key)), GUILayout.Width(EditorGUIUtility.labelWidth));
            int value = Mathf.Clamp(Mathf.RoundToInt(GUILayout.HorizontalSlider(current, 8f, 32f)), 8, 32);
            EditorGUILayout.LabelField(value.ToString(), EditorStyles.miniLabel, GUILayout.Width(24f));

            if (sizePreviewStyle == null)
            {
                sizePreviewStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }

            sizePreviewStyle.fontSize = value;
            GUILayout.Label(preview, sizePreviewStyle, GUILayout.Width(90f), GUILayout.Height(Mathf.Max(EditorGUIUtility.singleLineHeight, value + 2f)));
            EditorGUILayout.EndHorizontal();

            if (value == current) return false;
            apply(value);
            return true;
        }

        private static bool DescriptionMarkersList()
        {
            EditorGUILayout.LabelField(new GUIContent(
                EditorAnnotationLocalization.Text(EAnnotationText.DescriptionMarkers),
                EditorAnnotationLocalization.Text(EAnnotationText.DescriptionMarkersTooltip)));

            var markers = new List<string>(EditorAnnotationSettings.DescriptionFiltersText.Split('\n'));
            bool changed = false;
            for (int i = 0; i < markers.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                string marker = EditorGUILayout.TextField(markers[i]);
                if (marker != markers[i])
                {
                    markers[i] = marker;
                    changed = true;
                }

                if (GUILayout.Button(EditorAnnotationLocalization.Text(EAnnotationText.RemoveMarker), GUILayout.Width(72f)))
                {
                    markers.RemoveAt(i);
                    changed = true;
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button(EditorAnnotationLocalization.Text(EAnnotationText.AddMarker)))
            {
                markers.Add(string.Empty);
                changed = true;
            }

            if (changed)
                EditorAnnotationSettings.DescriptionFiltersText = string.Join("\n", markers);

            return changed;
        }

        private static bool BoolField(string key, string label, bool current, System.Action<bool> apply)
        {
            var value = EditorGUILayout.Toggle(new GUIContent(label, GetTooltip(key)), current);
            if (value == current) return false;
            apply(value);
            return true;
        }

        private static bool IntField(string key, string label, int current, System.Action<int> apply)
        {
            var value = EditorGUILayout.IntField(new GUIContent(label, GetTooltip(key)), current);
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
            var options = new[]
            {
                EditorAnnotationLocalization.Text(EAnnotationText.ClickNone),
                EditorAnnotationLocalization.Text(EAnnotationText.ClickSingle),
                EditorAnnotationLocalization.Text(EAnnotationText.ClickDouble)
            };
            int selected = (int)current;
            if (selected < 0 || selected >= options.Length)
                selected = 0;
            var value = (EClickType)EditorGUILayout.Popup(new GUIContent(label, GetTooltip(key)), selected, options);
            if (value == current) return false;
            apply(value);
            return true;
        }

        private static string GetTooltip(string key) =>
            EditorAnnotationLocalization.Text(EAnnotationText.PreferenceTooltip)
                .Replace("{0}", EditorAnnotationSettings.PrefKey(key));
    }

    internal enum EAnnotationLanguage
    {
        Chinese,
        English
    }

    internal enum EAnnotationText
    {
        SettingsPageLabel,
        SwitchLanguage,
        AssetSection,
        TitleSize,
        EditOnClick,
        SceneSection,
        ScriptSection,
        Color,
        Bold,
        Italic,
        ScanMaxLines,
        DescriptionMarkers,
        DescriptionMarkersTooltip,
        AddMarker,
        RemoveMarker,
        SizePreview,
        PreferenceTooltip,
        ClickNone,
        ClickSingle,
        ClickDouble,
        AssetWindowTitle,
        SceneWindowTitle,
        Target,
        Title,
        Description,
        RichTextGuide,
        RichBoldExample,
        RichItalicExample,
        RichSizeExample,
        RichColorExample,
        Save,
        SaveAndClose,
        Cancel,
        Delete,
        Preview,
        EmptyPreview,
        ToolbarAnnotation,
        ToolbarAnnotationTooltip,
        TimeScaleLabel,
        TimeScaleTooltip
    }

    internal static class EditorAnnotationLocalization
    {
        private static readonly Dictionary<EAnnotationText, (string chinese, string english)> Texts =
            new Dictionary<EAnnotationText, (string chinese, string english)>
            {
                { EAnnotationText.SettingsPageLabel, ("Lin 编辑器绘制", "Lin Editor Drawer") },
                { EAnnotationText.SwitchLanguage, ("English", "中文") },
                { EAnnotationText.AssetSection, ("资源注释（Project 窗口）", "Asset annotations (Project window)") },
                { EAnnotationText.TitleSize, ("注释字体大小", "Annotation font size") },
                { EAnnotationText.EditOnClick, ("点击注释修改", "Edit annotation on click") },
                { EAnnotationText.SceneSection, ("场景物体注释（Hierarchy 行内）", "Scene object annotations (Hierarchy)") },
                { EAnnotationText.ScriptSection, ("脚本注释（取自 .cs 源码头部注释）", "Script annotations (read from .cs headers)") },
                { EAnnotationText.Color, ("颜色", "Color") },
                { EAnnotationText.Bold, ("粗体", "Bold") },
                { EAnnotationText.Italic, ("斜体", "Italic") },
                { EAnnotationText.ScanMaxLines, ("每个脚本扫描最大行数", "Max scanned lines per script") },
                { EAnnotationText.DescriptionMarkers, ("描述标识", "Description markers") },
                { EAnnotationText.DescriptionMarkersTooltip, ("在 .cs 文件里识别描述行的前缀，一行一个", "Prefixes used to identify description lines in .cs files, one per line") },
                { EAnnotationText.AddMarker, ("添加标识", "Add marker") },
                { EAnnotationText.RemoveMarker, ("移除", "Remove") },
                { EAnnotationText.SizePreview, ("注释", "Note") },
                { EAnnotationText.PreferenceTooltip, ("存储于 EditorPrefs：{0}（本机本用户，不随工程进版本库）", "Stored in EditorPrefs: {0} (local to this user and machine; not stored with the project)") },
                { EAnnotationText.ClickNone, ("无响应", "None") },
                { EAnnotationText.ClickSingle, ("单击", "Single click") },
                { EAnnotationText.ClickDouble, ("双击", "Double click") },
                { EAnnotationText.AssetWindowTitle, ("文件注释", "Asset Annotation") },
                { EAnnotationText.SceneWindowTitle, ("场景物体注释", "Scene Object Annotation") },
                { EAnnotationText.Target, ("目标", "Target") },
                { EAnnotationText.Title, ("标题", "Title") },
                { EAnnotationText.Description, ("说明", "Description") },
                { EAnnotationText.RichTextGuide, ("富文本语法说明", "Rich text syntax") },
                { EAnnotationText.RichBoldExample, ("<b>文本</b> - 粗体", "<b>Text</b> - Bold") },
                { EAnnotationText.RichItalicExample, ("<i>文本</i> - 斜体", "<i>Text</i> - Italic") },
                { EAnnotationText.RichSizeExample, ("<size=14>文本</size> - 字体大小", "<size=14>Text</size> - Font size") },
                { EAnnotationText.RichColorExample, ("<color=#ff0000>文本</color> - 字体颜色", "<color=#ff0000>Text</color> - Font color") },
                { EAnnotationText.Save, ("保存", "Save") },
                { EAnnotationText.SaveAndClose, ("保存并关闭", "Save and Close") },
                { EAnnotationText.Cancel, ("取消", "Cancel") },
                { EAnnotationText.Delete, ("删除", "Delete") },
                { EAnnotationText.Preview, ("注释效果预览", "Annotation Preview") },
                { EAnnotationText.EmptyPreview, ("预览将在这里显示...", "Preview will appear here...") },
                { EAnnotationText.ToolbarAnnotation, ("注释", "Note") },
                { EAnnotationText.ToolbarAnnotationTooltip, ("打开注释设置", "Open annotation settings") },
                { EAnnotationText.TimeScaleLabel, ("时间倍率", "Time Scale") },
                { EAnnotationText.TimeScaleTooltip, ("TimeScale控制器", "Time Scale Controller") }
            };

        public static event System.Action LanguageChanged;

        public static bool IsEnglish => EditorAnnotationSettings.Language == EAnnotationLanguage.English;

        public static string Text(EAnnotationText key)
        {
            var pair = Texts[key];
            return IsEnglish ? pair.english : pair.chinese;
        }

        public static void ToggleLanguage()
        {
            EditorAnnotationSettings.Language = IsEnglish ? EAnnotationLanguage.Chinese : EAnnotationLanguage.English;
            LanguageChanged?.Invoke();
        }

        public static void SetLabel(VisualElement root, string name, EAnnotationText key)
        {
            var label = root.Q<Label>(name);
            if (label != null)
                label.text = Text(key);
        }
    }
}
