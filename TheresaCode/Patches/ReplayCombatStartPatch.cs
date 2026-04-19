using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Utils;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 战斗开始时重置重现限制（每场战斗重置一次）
/// </summary>
[HarmonyPatch(typeof(CombatRoom), "StartCombat")]
public static class ReplayCombatStartPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        ReplayKeyword.OnTurnStart();
        RemovedCardsTracker.OnCombatStart();
    }
}
