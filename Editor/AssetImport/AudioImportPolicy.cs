using System;
using UnityEditor;
using UnityEngine;
using Lin.Editor.Annotation.Settings;   // 形参类型：老计划写的是 AssetImportSettings，那个 SO 已被 M2 并进 AnnotationSettings

namespace Lin.Editor.AssetImport
{
    /// <summary>音频资源的角色，由路径关键字判定。</summary>
    public enum AudioRole { Bgm, Voice, Sfx }

    internal readonly struct AudioSampleDecision
    {
        public readonly AudioClipLoadType loadType;
        public readonly AudioCompressionFormat format;
        public readonly float quality;

        public AudioSampleDecision(AudioClipLoadType loadType, AudioCompressionFormat format, float quality)
        {
            this.loadType = loadType;
            this.format = format;
            this.quality = quality;
        }
    }

    /// <summary>
    /// 音频导入策略。原生组与 Web 组的分歧来自两处文档约束：
    /// WebGL 只支持 CompressedInMemory / DecompressOnLoad，且 Vorbis 解压加载吃约 10 倍内存；
    /// 而 PCM 是文档给短音效的推荐，代价是包体，所以只在原生组用。
    /// </summary>
    public static class AudioImportPolicy
    {
        internal static AudioRole Classify(string assetPath, AnnotationSettings settings)
        {
            if (string.IsNullOrEmpty(assetPath))
                return AudioRole.Sfx;

            if (ContainsAny(assetPath, settings.bgmKeywords))
                return AudioRole.Bgm;
            if (ContainsAny(assetPath, settings.voiceKeywords))
                return AudioRole.Voice;
            return AudioRole.Sfx;
        }

        internal static AudioSampleDecision Resolve(AudioRole role, string platform, AnnotationSettings settings)
        {
            float quality = role switch
            {
                AudioRole.Bgm => 0.8f,
                AudioRole.Voice => 0.6f,
                _ => 0.5f,
            };

            // Web / 小游戏：流送不可用，大文件不能解压加载 → 三类统一 CompressedInMemory
            if (settings.IsWebPlatform(platform))
                return new AudioSampleDecision(AudioClipLoadType.CompressedInMemory, AudioCompressionFormat.Vorbis, quality);

            return role switch
            {
                AudioRole.Bgm => new AudioSampleDecision(AudioClipLoadType.Streaming, AudioCompressionFormat.Vorbis, quality),
                AudioRole.Voice => new AudioSampleDecision(AudioClipLoadType.CompressedInMemory, AudioCompressionFormat.Vorbis, quality),
                // PCM 下 quality 被忽略（文档："Doesn't apply to PCM/ADPCM/HEVAG formats"），照写不误
                _ => new AudioSampleDecision(AudioClipLoadType.DecompressOnLoad, AudioCompressionFormat.PCM, quality),
            };
        }

        private static bool ContainsAny(string haystack, string[] needles)
        {
            if (needles == null)
                return false;

            for (int i = 0; i < needles.Length; i++)
                if (!string.IsNullOrEmpty(needles[i]) &&
                    haystack.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

            return false;
        }
    }
}
