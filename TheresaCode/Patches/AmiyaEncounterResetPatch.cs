using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using Theresa.TheresaCode.Events;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 新游戏开始时重置阿米娅事件的触发状态
/// </summary>
[HarmonyPatch(typeof(RunState), nameof(RunState.CreateForNewRun))]
public static class AmiyaEncounterResetPatch
{
    public static void Prefix(IReadOnlyList<Player> players, IReadOnlyList<ActModel> acts, IReadOnlyList<ModifierModel> modifiers, int ascensionLevel, string seed)
    {
        AmiyaEncounterEvent.ResetTriggerState();
        AmiyaEncounterForceSpawnPatch.ResetState();
        MainFile.Logger?.Info("[AmiyaEncounterResetPatch] 新游戏开始，重置阿米娅事件触发状态");
    }
}

/// <summary>
/// 测试模式新游戏开始时也重置阿米娅事件的触发状态
/// </summary>
[HarmonyPatch(typeof(RunState), nameof(RunState.CreateForTest))]
public static class AmiyaEncounterResetPatchForTest
{
    public static void Prefix(IReadOnlyList<Player>? players, IReadOnlyList<ActModel>? acts, IReadOnlyList<ModifierModel>? modifiers, int ascensionLevel, string? seed)
    {
        AmiyaEncounterEvent.ResetTriggerState();
        AmiyaEncounterForceSpawnPatch.ResetState();
        MainFile.Logger?.Info("[AmiyaEncounterResetPatchForTest] 测试模式新游戏开始，重置阿米娅事件触发状态");
    }
}
