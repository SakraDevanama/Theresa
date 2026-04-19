using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Cards;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 监听玩家受到伤害，增加手牌中"宽恕"卡牌的 MagicNumber
/// 使用 Harmony Patch 直接拦截 Hook.AfterDamageReceived，比 Power 的回调更可靠
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDamageReceived))]
public static class ForgiveDamagePatch
{
    private static void Prefix(Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 只处理玩家受到的实际伤害（未被格挡的部分）
        if (target?.Player is not Player player) return;
        if (result.UnblockedDamage <= 0) return;

        // 日志已移除

        // 获取手牌中的 Forgive 卡牌
        var handPile = player.PlayerCombatState?.AllPiles.FirstOrDefault(p => p.Type == MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand);
        if (handPile == null)
        {
            // 日志已移除
            return;
        }

        int forgiveCount = 0;
        foreach (var card in handPile.Cards)
        {
            if (card is Forgive forgiveCard)
            {
                forgiveCard.IncrementMagicNumber();
                forgiveCount++;
            }
        }

        // 日志已移除
    }
}
