using Lin.Editor.Annotation.Settings;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lin.Editor.Annotation.Asset
{
    /// <summary>
    /// 在 Project 窗口绘制资源/文件夹注释；.cs 文件额外从源码头部注释里提取一行作为脚本注释。
    /// </summary>
    [InitializeOnLoad]
    public static class AssetSummaryDrawer
    {
        private const float DISPLAY_OFFSET = 25;

        private static Dictionary<string, (string description, string tooltip)> descriptionMap;
        private static HashSet<string> readedList;

        // 双击检测相关字段
        private static float lastClickTime = 0f;
        private static Vector2 lastClickPosition = Vector2.zero;
        private static string lastClickedGuid = string.Empty;
        private const float DOUBLE_CLICK_TIME = 0.3f;
        private const float DOUBLE_CLICK_DISTANCE = 5f;

        static AssetSummaryDrawer()
        {
            readedList = new HashSet<string>();
            descriptionMap = new Dictionary<string, (string description, string tooltip)>();
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemOnGUI;
        }

        [MenuItem("Assets/Edit Annotation - 修改注释")]
        private static void SetAssetDescription()
        {
            var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            AssetSummaryWindow.ShowAssetSummary(assetPath);
        }

        private static void OnProjectWindowItemOnGUI(string guid, Rect selectionRect)
        {
            if (string.IsNullOrEmpty(guid))
                return;

            if (descriptionMap.ContainsKey(guid))
            {
                var labelRect = new Rect(selectionRect);
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(assetPath);

                // 创建样式并启用富文本
                var style = new GUIStyle(EditorStyles.miniLabel);
                style.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 1f);
                style.richText = true;
                style.fontSize = EditorAnnotationSettings.AssetSummaryTitleSize;
                style.alignment = TextAnchor.MiddleRight;

                // 计算文件名宽度
                string description = descriptionMap[guid].description;
                float fileNameWidth = EditorStyles.label.CalcSize(new GUIContent(fileName)).x;

                // 列表项很扁，网格项接近方形；按条目矩形判断，避免窄分栏把列表误判成网格。
                float lineHeight = EditorGUIUtility.singleLineHeight;
                bool isGridView = selectionRect.height > lineHeight * 2f ||
                                  selectionRect.width < selectionRect.height * 4f;
                if (isGridView)
                {
                    float labelY = selectionRect.height > lineHeight * 2f
                        ? selectionRect.yMax - lineHeight * 2f
                        : selectionRect.yMax + 2f;
                    labelRect = new Rect(selectionRect.x + 2f, labelY, Mathf.Max(0f, selectionRect.width - 4f), lineHeight);
                }
                else
                {
                    float nameEndX = selectionRect.x + fileNameWidth + DISPLAY_OFFSET;
                    float availableWidth = Mathf.Max(0f, selectionRect.xMax - nameEndX - 5f);
                    labelRect.width = Mathf.Min(style.CalcSize(new GUIContent(description)).x, availableWidth);
                    labelRect.x = selectionRect.xMax - labelRect.width - 5f;
                }

                // 对象Tooltip
                var content = new GUIContent(description);
                content.tooltip = descriptionMap[guid].tooltip;

                // 绘制注释
                GUI.Label(labelRect, content, style);

                // 点击修改
                var currentEvent = Event.current;
                if (currentEvent.type == EventType.MouseDown)
                {
                    if (labelRect.Contains(currentEvent.mousePosition))
                    {
                        switch (EditorAnnotationSettings.AssetSummaryEditWay)
                        {
                            case EClickType.无响应:
                                break;

                            case EClickType.单击:
                                AssetSummaryWindow.ShowAssetSummary(AssetDatabase.GUIDToAssetPath(guid));
                                currentEvent.Use();
                                break;

                            case EClickType.双击:
                            default:
                                // 双击检测逻辑
                                float currentTime = (float)EditorApplication.timeSinceStartup;
                                Vector2 currentPosition = currentEvent.mousePosition;

                                // 检查是否为双击
                                if (currentTime - lastClickTime <= DOUBLE_CLICK_TIME &&
                                    Vector2.Distance(currentPosition, lastClickPosition) <= DOUBLE_CLICK_DISTANCE &&
                                    lastClickedGuid == guid)
                                {
                                    // 双击事件
                                    AssetSummaryWindow.ShowAssetSummary(AssetDatabase.GUIDToAssetPath(guid));

                                    // 重置双击检测状态
                                    lastClickTime = 0f;
                                    lastClickPosition = Vector2.zero;
                                    lastClickedGuid = string.Empty;
                                    currentEvent.Use();
                                }
                                else
                                {
                                    // 记录当前点击信息
                                    lastClickTime = currentTime;
                                    lastClickPosition = currentPosition;
                                    lastClickedGuid = guid;
                                }
                                break;
                        }
                    }
                }
            }
            else if (!readedList.Contains(guid))
            {
                readedList.Add(guid);

                var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if (importer == null)
                    return;

                var summary = importer.GetDescription();
                string title = summary.title;
                if (!string.IsNullOrEmpty(title))
                    title = summary.GetRichTitle();

                if (importer.assetPath.EndsWith(".cs"))
                {
                    // 读取.cs文件内容，按每个标识各提取一次（多个标识同时命中时会各加一段前缀，与原实现一致）
                    string fileContent = File.ReadAllText(importer.assetPath);

                    foreach (var filter in EditorAnnotationSettings.DescriptionFilters)
                        FindDescriptions(filter);

                    void FindDescriptions(string filter)
                    {
                        if (!fileContent.Contains(filter))
                            return;

                        var descMatch = System.Text.RegularExpressions.Regex.Match(
                            fileContent,
                            @$"{filter}([^\n]+)");

                        if (!descMatch.Success)
                            return;

                        string descValue = descMatch.Groups[1].Value.Trim();
                        if (string.IsNullOrWhiteSpace(descValue))
                            return;

                        descValue = $"<size={EditorAnnotationSettings.AssetSummaryTitleSize}>" +
                                    $"<color=#{ColorUtility.ToHtmlStringRGB(EditorAnnotationSettings.ScriptDescriptionColor)}>{descValue}</color>" +
                                    $"</size>";
                        if (EditorAnnotationSettings.ScriptDescriptionBold)
                            descValue = $"<b>{descValue}</b>";

                        if (EditorAnnotationSettings.ScriptDescriptionItalic)
                            descValue = $"<i>{descValue}</i>";

                        title = $"{descValue} {title}";
                    }
                }

                if (!string.IsNullOrEmpty(title))
                    descriptionMap.Add(guid, (title, summary.description));
            }
        }

        public static void Refresh(string guid)
        {
            descriptionMap.Remove(guid);
            readedList.Remove(guid);
        }

        public static void Refresh()
        {
            descriptionMap.Clear();
            readedList.Clear();
        }
    }
}
