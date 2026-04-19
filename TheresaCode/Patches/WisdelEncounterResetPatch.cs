using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using Theresa.TheresaCode.Events;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 新游戏开始时重置维什戴尔事件的触发状态
/// </summary>
[HarmonyPatch(typeof(RunState), nameof(RunState.CreateForNewRun))]
public static class WisdelEncounterResetPatch
{
    public static void Prefix(IReadOnlyList<Player> players, IReadOnlyList<ActModel> acts, IReadOnlyList<ModifierModel> modifiers, int ascensionLevel, string seed)
    {
        WisdelEncounterEvent.ResetTriggerState();
        WisdelEncounterForceSpawnPatch.ResetState();
        MainFile.Logger?.Info("[WisdelEncounterResetPatch] 新游戏开始，重置维什戴尔事件触发状态");
    }
}

/// <summary>
/// 测试模式新游戏开始时也重置维什戴尔事件的触发状态
/// </summary>
[HarmonyPatch(typeof(RunState), nameof(RunState.CreateForTest))]
public static class WisdelEncounterResetPatchForTest
{
    public static void Prefix(IReadOnlyList<Player>? players, IReadOnlyList<ActModel>? acts, IReadOnlyList<ModifierModel>? modifiers, int ascensionLevel, string? seed)
    {
        WisdelEncounterEvent.ResetTriggerState();
        WisdelEncounterForceSpawnPatch.ResetState();
        MainFile.Logger?.Info("[WisdelEncounterResetPatchForTest] 测试模式新游戏开始，重置维什戴尔事件触发状态");
    }
}
