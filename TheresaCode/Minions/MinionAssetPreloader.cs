using Godot;
using System.Collections.Generic;

namespace Theresa.TheresaCode.Minions;

/// <summary>
/// 随从资源预加载器
/// 
/// 在 Mod 初始化时预加载所有随从相关的 Spine 场景、纹理和音效到独立的静态缓存中，
/// 避免被 PreloadManager 的资源卸载机制清理，从而防止召唤时的首次加载卡顿。
/// </summary>
public static class MinionAssetPreloader
{
    // 独立的静态缓存，不受 PreloadManager 资源卸载的影响
    private static readonly Dictionary<string, PackedScene> _sceneCache = new();
    private static readonly Dictionary<string, Texture2D> _textureCache = new();
    private static readonly Dictionary<string, AudioStream> _audioCache = new();
    private static readonly HashSet<string> _preloadedPaths = new();

    /// <summary>
    /// 随从 Spine 场景根目录
    /// </summary>
    public const string MinionScenesRoot = "res://Theresa/animations/Minion";

    /// <summary>
    /// 随从音效根目录
    /// </summary>
    public const string MinionAudioRoot = "res://Theresa/audio";

    /// <summary>
    /// 预加载所有随从资源（Spine场景 + 音效）
    /// </summary>
    public static void PreloadAll()
    {
        int count = 0;

        // 1. 预加载随从 Spine 场景 (.tscn)
        var scenePaths = new List<string>
        {
            $"{MinionScenesRoot}/Amiya.tscn",
            $"{MinionScenesRoot}/Swordsman.tscn",
            $"{MinionScenesRoot}/wisdel.tscn"
        };
        foreach (var path in scenePaths)
        {
            if (PreloadScene(path))
                count++;
        }

        // 2. 预加载随从 Spine 数据资源 (.tres)
        var tresPaths = new List<string>
        {
            $"{MinionScenesRoot}/Amiya.tres",
            $"{MinionScenesRoot}/Swordsman.tres",
            $"{MinionScenesRoot}/wisdel.tres"
        };
        foreach (var path in tresPaths)
        {
            if (PreloadResource<Resource>(path))
                count++;
        }

        // 3. 预加载 Spine Atlas 资源
        var atlasPaths = new List<string>
        {
            $"{MinionScenesRoot}/char_1037_amiya3.atlas",
            $"{MinionScenesRoot}/enemy_2081_skztxs.atlas",
            $"{MinionScenesRoot}/char_1035_wisdel_sale#14.atlas"
        };
        foreach (var path in atlasPaths)
        {
            if (PreloadResource<Resource>(path))
                count++;
        }

        // 4. 预加载 Spine Skeleton 资源
        var skelPaths = new List<string>
        {
            $"{MinionScenesRoot}/char_1037_amiya3.skel",
            $"{MinionScenesRoot}/enemy_2081_skztxs.skel",
            $"{MinionScenesRoot}/char_1035_wisdel_sale#14.skel"
        };
        foreach (var path in skelPaths)
        {
            if (PreloadResource<Resource>(path))
                count++;
        }

        // 5. 预加载 Spine 纹理
        var texturePaths = new List<string>
        {
            $"{MinionScenesRoot}/char_1037_amiya3.png",
            $"{MinionScenesRoot}/enemy_2081_skztxs.png",
            $"{MinionScenesRoot}/char_1035_wisdel_sale#14.png"
        };
        foreach (var path in texturePaths)
        {
            if (PreloadTexture(path))
                count++;
        }

        // 6. 预加载所有随从音效
        var audioPaths = new List<string>
        {
            // 阿米娅音效
            $"{MinionAudioRoot}/Theresa_fo_Amiya.wav",
            $"{MinionAudioRoot}/Amiya_1.wav",
            $"{MinionAudioRoot}/Amiya_2.wav",
            $"{MinionAudioRoot}/Amiya_atk.wav",
            $"{MinionAudioRoot}/Amiya_heal.wav",
            // 维什戴尔音效
            $"{MinionAudioRoot}/wisdel_1.wav",
            $"{MinionAudioRoot}/wisdel_2.wav",
            $"{MinionAudioRoot}/wisdel_atk_1.wav",
            $"{MinionAudioRoot}/wisdel_skill_1.wav",
            $"{MinionAudioRoot}/wisdel_skill_2.wav",
            $"{MinionAudioRoot}/wisdel_slash1.wav",
            $"{MinionAudioRoot}/wisdel_slash2.wav",
            $"{MinionAudioRoot}/wisdel_slash3.wav",
            // 特雷西斯音效
            $"{MinionAudioRoot}/swordsman_slash.wav",
            $"{MinionAudioRoot}/swordsman_slash2.wav"
        };
        foreach (var path in audioPaths)
        {
            if (PreloadAudio(path))
                count++;
        }

        MainFile.Logger?.Info($"[MinionAssetPreloader] Preloaded {count} minion assets.");
    }

    /// <summary>
    /// 预加载单个场景到独立缓存
    /// </summary>
    private static bool PreloadScene(string path)
    {
        if (_preloadedPaths.Contains(path))
            return false;

        try
        {
            var scene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Reuse);
            if (scene != null)
            {
                _sceneCache[path] = scene;
                _preloadedPaths.Add(path);
                return true;
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger?.Warn($"[MinionAssetPreloader] Failed to preload scene {path}: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// 预加载通用资源到独立缓存
    /// </summary>
    private static bool PreloadResource<T>(string path) where T : class
    {
        if (_preloadedPaths.Contains(path))
            return false;

        try
        {
            var resource = ResourceLoader.Load<T>(path, null, ResourceLoader.CacheMode.Reuse);
            if (resource != null)
            {
                _preloadedPaths.Add(path);
                return true;
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger?.Warn($"[MinionAssetPreloader] Failed to preload resource {path}: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// 预加载单个纹理到独立缓存
    /// </summary>
    private static bool PreloadTexture(string path)
    {
        if (_preloadedPaths.Contains(path))
            return false;

        try
        {
            var texture = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
            if (texture != null)
            {
                _textureCache[path] = texture;
                _preloadedPaths.Add(path);
                return true;
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger?.Warn($"[MinionAssetPreloader] Failed to preload texture {path}: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// 预加载单个音频到独立缓存
    /// </summary>
    private static bool PreloadAudio(string path)
    {
        if (_preloadedPaths.Contains(path))
            return false;

        try
        {
            var stream = ResourceLoader.Load<AudioStream>(path, null, ResourceLoader.CacheMode.Reuse);
            if (stream != null)
            {
                _audioCache[path] = stream;
                _preloadedPaths.Add(path);
                return true;
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger?.Warn($"[MinionAssetPreloader] Failed to preload audio {path}: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// 尝试从预加载缓存获取场景
    /// </summary>
    public static PackedScene? GetPreloadedScene(string path)
    {
        if (_sceneCache.TryGetValue(path, out var scene))
        {
            return scene;
        }
        return null;
    }

    /// <summary>
    /// 尝试从预加载缓存获取纹理
    /// </summary>
    public static Texture2D? GetPreloadedTexture(string path)
    {
        if (_textureCache.TryGetValue(path, out var texture))
        {
            return texture;
        }
        return null;
    }

    /// <summary>
    /// 尝试从预加载缓存获取音频
    /// </summary>
    public static AudioStream? GetPreloadedAudio(string path)
    {
        if (_audioCache.TryGetValue(path, out var stream))
        {
            return stream;
        }
        return null;
    }
}
