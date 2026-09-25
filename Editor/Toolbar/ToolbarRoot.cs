using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lin.Editor.Toolbar
{
    [InitializeOnLoad]
    public static class ToolbarRoot
    {
        public static VisualElement leftAlign { get; private set; }
        public static VisualElement middleAlign { get; private set; }
        public static VisualElement rightAlign { get; private set; }

        static ToolbarRoot()
        {
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
#if UNITY_6000_5_OR_NEWER
            if (!MountMainToolbarWindow())
#else
            if (!MountLegacyToolbar())
#endif
                return;

            EditorApplication.update -= OnUpdate;
        }

#if UNITY_6000_5_OR_NEWER
        // Unity 6000.5 起主工具栏重建为 UI Toolkit 的 UnityEditor.MainToolbarWindow（EditorWindow），
        // 旧的 UnityEditor.Toolbar 类型与 m_Root/ToolbarZone* 挂载点已移除；官方 MainToolbarElement 是抽象类且
        // CreateElement 为 internal，无法承载任意 VisualElement，因此改为挂到 overlay 内容容器：
        // rootVisualElement → #overlay-toolbar__top → 三个分区（before/middle/after-spacer-container）各带 .unity-overlay-container__content。
        // 左/中锚定在中间分区 #PlayMode（播放按钮组）之前、之后；右挂到右侧分区（Unity 自带 Layers/Layout 等按钮所在）末尾。
        // 中间容器与播放按钮组的可视间距要求 15px：元素自身 marginLeft 4px（ToolbarElements）+ 按钮前 GUILayout.Space(5)（ToolbarButton）已占 9px。
        private const float PlayModeGap = 15f;
        private const float ElementInnerOffset = 4f + 5f;

        private static bool MountMainToolbarWindow()
        {
            var windowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.MainToolbarWindow");
            if (windowType == null)
                return false;

            var windows = Resources.FindObjectsOfTypeAll(windowType);
            if (windows.Length == 0)
                return false;

            var window = windows[0] as EditorWindow;
            if (window == null || window.rootVisualElement == null)
                return false;

            var playMode = window.rootVisualElement.Q("PlayMode");
            var content = playMode != null
                ? playMode.parent
                : window.rootVisualElement.Q(className: "unity-overlay-container__content");
            if (content == null)
                return false;

            var afterSection = window.rootVisualElement.Q(className: "unity-overlay-container__after-spacer-container");
            var rightContent = afterSection?.Q(className: "unity-overlay-container__content") ?? content;

            leftAlign = new VisualElement();
            leftAlign.name = "CustomLeftAlign";

            middleAlign = new VisualElement();
            middleAlign.name = "CustomMiddleAlign";
            middleAlign.style.marginLeft = PlayModeGap - ElementInnerOffset;

            if (playMode != null)
            {
                content.Insert(content.IndexOf(playMode), leftAlign);
                content.Insert(content.IndexOf(playMode) + 1, middleAlign);
            }
            else
            {
                content.Add(leftAlign);
                content.Add(middleAlign);
            }

            rightAlign = new VisualElement();
            rightAlign.name = "CustomRightAlign";
            rightContent.Add(rightAlign);

            return true;
        }
#else
        private static bool MountLegacyToolbar()
        {
            var barType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
            var bars = Resources.FindObjectsOfTypeAll(barType);
            if (bars.Length == 0)
                return false;

            var toolbar = bars[0] as ScriptableObject;

            // 反射获取toolbar的m_Root
            var rootField = barType.GetField("m_Root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var root = (rootField.GetValue(toolbar) as VisualElement).Q("ToolbarContainerContent");
            leftAlign = new VisualElement();
            leftAlign.name = "CustomLeftAlign";
            root.Q("ToolbarZoneLeftAlign").Add(leftAlign);

            middleAlign = new VisualElement();
            middleAlign.name = "CustomMiddleAlign";
            root.Q("ToolbarZonePlayMode").Add(middleAlign);

            rightAlign = new VisualElement();
            rightAlign.name = "CustomRightAlign";
            root.Q("ToolbarZoneRightAlign").Add(rightAlign);

            return true;
        }
#endif
    }
}
