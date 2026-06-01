using System.Reflection;
using BaseLib.Utils;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Minions;
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
        
        // 预加载随从资源（Spine场景 + 音效），避免召唤时同步加载导致卡顿
        MinionAssetPreloader.PreloadAll();
        
        Log.Debug("Mod initialized!");
    }
}
