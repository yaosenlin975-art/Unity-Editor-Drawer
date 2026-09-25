using System.IO;
using UnityEditorInternal;
using UnityEngine;

namespace Lin.Editor.Annotation.Settings
{
    /// <summary>
    /// 包设置，落在宿主工程 ProjectSettings/ 下、随工程进版本库。
    /// AssetDatabase 看不见这个对象，所以 SetDirty 无效——改动必须显式走 Save()。
    /// </summary>
    internal class AnnotationSettings : ScriptableObject
    {
        internal const string FilePath = "ProjectSettings/LinEditorDrawer.asset";
        internal const int CurrentVersion = 1;

        public int settingsVersion;

        public int assetSummaryTitleSize = 14;
        public EClickType assetSummaryEditWay = EClickType.单击;
        public int sceneObjectDescriptionTitleSize = 14;
        public EClickType sceneObjectDescriptionEditWay = EClickType.单击;

        // 门面原先存的是字符串 "#808080"，SO 直接存 Color
        public Color scriptDescriptionColor = new Color(128 / 255f, 128 / 255f, 128 / 255f, 1f);
        public bool scriptDescriptionBold;
        public bool scriptDescriptionItalic;
        public int scriptDescriptionScanMaxLines = 100;
        public string descriptionFiltersText = "Description: \nDescription：\n功能说明: ";

        private AnnotationSettings() => settingsVersion = CurrentVersion;

        private static AnnotationSettings instance;

        internal static AnnotationSettings Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                // 文件不存在时这个重载返回 null 还是空数组本地无证据（URP 那份把返回值丢了，不算证据），先判空
                var loaded = InternalEditorUtility.LoadSerializedFileAndForget(FilePath);
                if (loaded != null)
                {
                    foreach (var obj in loaded)
                    {
                        if (obj is AnnotationSettings found)
                        {
                            instance = found;
                            break;
                        }
                    }
                }

                if (instance == null)
                    instance = CreateInstance<AnnotationSettings>();

                instance.hideFlags = HideFlags.HideAndDontSave;

                if (instance.settingsVersion < CurrentVersion)
                {
                    instance.Migrate();
                    Save();
                }
                else if (!File.Exists(FilePath))
                    Save();

                return instance;
            }
        }

        /// <summary>
        /// settingsVersion 为 0 说明这份文件早于该字段：缺的字段反序列化填 0/false/空，而不是 C# 初始化值。
        /// 无法区分"用户真选了 无响应(0)"和"字段当时不存在"，所以整份回默认，认这个代价（ADR-001）。
        /// </summary>
        internal void Migrate()
        {
            var fresh = CreateInstance<AnnotationSettings>();
            assetSummaryTitleSize = fresh.assetSummaryTitleSize;
            assetSummaryEditWay = fresh.assetSummaryEditWay;
            sceneObjectDescriptionTitleSize = fresh.sceneObjectDescriptionTitleSize;
            sceneObjectDescriptionEditWay = fresh.sceneObjectDescriptionEditWay;
            scriptDescriptionColor = fresh.scriptDescriptionColor;
            scriptDescriptionBold = fresh.scriptDescriptionBold;
            scriptDescriptionItalic = fresh.scriptDescriptionItalic;
            scriptDescriptionScanMaxLines = fresh.scriptDescriptionScanMaxLines;
            descriptionFiltersText = fresh.descriptionFiltersText;
            settingsVersion = CurrentVersion;
        }

        internal static void Save()
        {
            if (instance == null)
                return;

            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // 不设 allowTextSerialization 会得到一个不可 diff、不可 merge 的二进制 .asset
            InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { instance }, FilePath, true);
        }
    }
}
