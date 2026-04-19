using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using Theresa.TheresaCode.Cards;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 战斗开始时，为手牌中所有"宽恕"卡牌创建 ForgiveTrackerPower
/// 解决战斗开始时卡牌已在手牌中，OnGlobalMove 不会触发的问题
/// </summary>
[HarmonyPatch(typeof(CombatRoom), "StartCombat")]
public static class ForgiveCombatStartPatch
{
    [HarmonyPostfix]
    public static void Postfix(CombatRoom __instance)
    {
        // 日志已移除

        if (__instance.CombatState == null)
        {
            // 日志已移除
            return;
        }

        foreach (var player in __instance.CombatState.Players)
        {
            if (player?.Creature == null) continue;

            // 检查是否已有追踪器
            var tracker = player.Creature.Powers.FirstOrDefault(p => p is ForgiveTrackerPower);
            if (tracker != null)
            {
                // 日志已移除
                continue;
            }

            // 通过 PlayerCombatState 获取手牌
            if (player.PlayerCombatState == null)
            {
                // 日志已移除
                continue;
            }

            var handPile = player.PlayerCombatState.AllPiles.FirstOrDefault(p => p.Type == PileType.Hand);
            if (handPile == null)
            {
                // 日志已移除
                continue;
            }

            bool hasForgive = handPile.Cards.Any(c => c is Forgive);
            if (!hasForgive)
            {
                // 日志已移除
                continue;
            }

            // 日志已移除
            _ = CreateTracker(player.Creature);
        }
    }

    private static async System.Threading.Tasks.Task CreateTracker(MegaCrit.Sts2.Core.Entities.Creatures.Creature creature)
    {
        await MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<ForgiveTrackerPower>(new ThrowingPlayerChoiceContext(), creature, 1, creature, null);
        // 日志已移除
    }
}
