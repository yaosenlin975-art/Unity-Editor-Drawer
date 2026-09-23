using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lin.Editor.Annotation.Settings;
using UnityEditor;
using UnityEngine;

namespace Lin.Editor.Annotation.Asset
{
    /// <summary>
    /// 注释的中心化存储：宿主工程 ProjectSettings 下一个 JSON，按资源 GUID 索引。
    /// 不写进 .meta，因此加注释不会让资源导入器变脏、也不会污染版本库里的资产文件。
    /// </summary>
    public class AssetSummaryArchiver
    {
        private static AssetSummaryArchiver instance;

        /// <summary>
        /// 读档失败时置位：此后禁止写盘，否则会把用户手写的注释整体抹掉。
        /// </summary>
        private bool loadFailed;

        private readonly Dictionary<string, AssetSummary> summaryMap = new Dictionary<string, AssetSummary>();

        private static readonly string JsonFilePath = Path.Combine("ProjectSettings", "LinEditorAnnotation.json");

        private AssetSummaryArchiver() => LoadFromJson();

        public static AssetSummaryArchiver GetInstance()
        {
            if (instance == null)
                instance = new AssetSummaryArchiver();
            return instance;
        }

        /// <summary>JsonUtility 不支持字典，落盘用记录列表、GUID 作为普通字段。</summary>
        [Serializable]
        private class Record
        {
            public string guid;
            public string title;
            public string titleColor;
            public string description;
            public string createTime;
            public string updateTime;
        }

        [Serializable]
        private class Archive
        {
            public List<Record> items = new List<Record>();
        }

        private void LoadFromJson()
        {
            try
            {
                if (!File.Exists(JsonFilePath))
                {
                    // 新工程上这是正常首态，用 Log 而不是 LogWarning：Editor 对 warning 也印整段堆栈，
                    // 首次打开 Project 窗口时会被它刷一条看起来像异常的日志。
                    Debug.Log("[Annotation] 未找到资源注释数据文件，将创建新的数据集");
                    return;
                }

                var parsed = JsonUtility.FromJson<Archive>(File.ReadAllText(JsonFilePath));

                // items 为 null 说明文件有内容但不是本包的格式（例如手写坏了、或被别的工具覆写）。
                // 此时必须停写：把它当空档继续跑，第一次保存就会把原注释整体抹掉。
                if (parsed?.items == null)
                {
                    loadFailed = true;
                    LogError($"{JsonFilePath} 结构无法识别（缺少 items 数组），本次按空数据集处理且不会写盘。");
                    return;
                }

                foreach (var item in parsed.items)
                {
                    summaryMap[item.guid] = new AssetSummary
                    {
                        title = item.title,
                        titleColor = item.titleColor,
                        description = item.description,
                        createTime = item.createTime,
                        updateTime = item.updateTime,
                    };
                }
            }
            catch (Exception ex)
            {
                loadFailed = true;
                LogError($"加载资源注释数据失败, 本次按空数据集处理且不会写盘（原文件未改动）: {ex.Message}");
                summaryMap.Clear();
            }
        }

        public void SetDescription(AssetImporter importer, AssetSummary summary)
        {
            var guid = importer.GetAssetGUID();
            if (summaryMap.ContainsKey(guid))
            {
                summary.createTime = summaryMap[guid].createTime;
                summary.updateTime = Now;
                summaryMap[guid] = summary;
            }
            else
            {
                summary.createTime = Now;
                summary.updateTime = Now;
                summaryMap.Add(guid, summary);
            }

            AssetSummaryDrawer.Refresh(guid);
            Save();
        }

        public void RemoveDescription(string guid)
        {
            if (!summaryMap.Remove(guid))
                return;

            AssetSummaryDrawer.Refresh(guid);
            Save();
        }

        public void RemoveDescription(AssetImporter importer) => RemoveDescription(importer.GetAssetGUID());

        public AssetSummary Get(AssetImporter importer) =>
            summaryMap.TryGetValue(importer.GetAssetGUID(), out var summary) ? summary : default;

        public AssetSummary Get(string assetPath) =>
            summaryMap.TryGetValue(AssetDatabase.AssetPathToGUID(assetPath), out var summary) ? summary : default;

        private static string Now => DateTime.Now.ToString("o");

        private void Save()
        {
            if (loadFailed)
            {
                LogError("资源注释数据处于只读保护中（读档失败），跳过写盘。修复或删除 ProjectSettings/LinEditorAnnotation.json 后重启编辑器。");
                return;
            }

            try
            {
                var archive = new Archive
                {
                    items = summaryMap.Select(pair => new Record
                    {
                        guid = pair.Key,
                        title = pair.Value.title,
                        titleColor = pair.Value.titleColor,
                        description = pair.Value.description,
                        createTime = pair.Value.createTime,
                        updateTime = pair.Value.updateTime,
                    }).ToList()
                };

                Directory.CreateDirectory(Path.GetDirectoryName(JsonFilePath));
                File.WriteAllText(JsonFilePath, JsonUtility.ToJson(archive, true));
            }
            catch (Exception ex)
            {
                LogError($"保存资源注释数据失败: {ex.Message}");
            }
        }

        [MenuItem("Lin/Editor Annotation/清除已删除资源的注释")]
        private static void RemoveAllMissFileSummaryChinese() => RemoveDeletedAssetSummaries();

        [MenuItem("Lin/Editor Annotation/Clear Deleted Asset Annotations")]
        private static void RemoveAllMissFileSummaryEnglish() => RemoveDeletedAssetSummaries();

        [MenuItem("Lin/Editor Annotation/清除已删除资源的注释", true)]
        private static bool ValidateRemoveAllMissFileSummaryChinese() => !EditorAnnotationLocalization.IsEnglish;

        [MenuItem("Lin/Editor Annotation/Clear Deleted Asset Annotations", true)]
        private static bool ValidateRemoveAllMissFileSummaryEnglish() => EditorAnnotationLocalization.IsEnglish;

        private static void RemoveDeletedAssetSummaries()
        {
            var self = GetInstance();
            // 必须同时判目录：File.Exists 对文件夹恒为 false，只判文件会把打在文件夹上的注释整体删掉
            var toRemove = self.summaryMap.Keys
                .Where(guid =>
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    return !File.Exists(path) && !Directory.Exists(path);
                })
                .ToArray();

            foreach (var guid in toRemove)
                self.summaryMap.Remove(guid);

            self.Save();
            AssetSummaryDrawer.Refresh();
            Debug.Log($"[Annotation] 已移除 {toRemove.Length} 条注释");
        }

        private static void LogError(string message) => Debug.LogError($"[Annotation] {message}");
    }

    internal static class AssetImporterAnnotationExtensions
    {
        public static string GetAssetGUID(this AssetImporter self) => AssetDatabase.AssetPathToGUID(self.assetPath);

        public static AssetSummary GetDescription(this AssetImporter self) =>
            AssetSummaryArchiver.GetInstance().Get(self);
    }
}
