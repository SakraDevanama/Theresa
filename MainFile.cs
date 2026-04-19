using System.Reflection;
using BaseLib.Utils;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Patches;

namespace Theresa;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Theresa"; //At the moment, this is used only for the Logger and harmony names.

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);

        harmony.PatchAll(Assembly.GetExecutingAssembly());
        // 使得tscn可以加载自定义脚本
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(MainFile).Assembly);
        
        // 初始化微尘系统
        DustInitializer.Initialize();
        
        // 初始化 UnknownRelic Action 计数补丁
        UnknownRelicActionCounterPatch.Initialize();
        
        // 预加载随从音效资源，避免召唤时同步加载导致卡顿
        PreloadMinionAudio();
        
        Log.Debug("Mod initialized!");
    }

    /// <summary>
    /// 预加载阿米娅和维什戴尔随从的音效资源
    /// </summary>
    private static void PreloadMinionAudio()
    {
        try
        {
            var amiyaSounds = new[]
            {
                "res://Theresa/audio/Theresa_fo_Amiya.wav",
                "res://Theresa/audio/Amiya_1.wav",
                "res://Theresa/audio/Amiya_2.wav"
            };
            var wisdelSounds = new[]
            {
                "res://Theresa/audio/wisdel_1.wav",
                "res://Theresa/audio/wisdel_2.wav"
            };

            foreach (var path in amiyaSounds)
            {
                var stream = GD.Load<AudioStream>(path);
                if (stream != null)
                {
                    Theresa.TheresaCode.Minions.Models.AmiyaMinion.AudioCache[path] = stream;
                }
            }

            foreach (var path in wisdelSounds)
            {
                var stream = GD.Load<AudioStream>(path);
                if (stream != null)
                {
                    Theresa.TheresaCode.Minions.Models.WisdelMinion.AudioCache[path] = stream;
                }
            }

            Logger?.Info("[MainFile] Preloaded minion audio resources.");
        }
        catch (Exception ex)
        {
            Logger?.Warn($"[MainFile] Failed to preload minion audio: {ex.Message}");
        }
    }
}
