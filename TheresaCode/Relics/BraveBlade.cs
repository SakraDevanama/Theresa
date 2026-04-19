using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Commands;
using Theresa.TheresaCode.Enchantments;

namespace Theresa.TheresaCode.Relics;

/// <summary>
/// 无惧者巨刃 (BraveBlade)
/// Boss 遗物
/// 
/// 效果：
/// 1. 你从拥有丝线的卡牌造成的伤害提升33%。
/// 2. 在你回合开始时，对手中随机攻击牌编织：意志。
/// </summary>
[Pool(typeof(TheresaRelicPool))]
public sealed class BraveBlade : TheresaRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    /// <summary>
    /// 回合开始时：对手中随机一张攻击牌编织意志丝线
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player.NetId != Owner.NetId)
            return;

        var handCards = PileType.Hand.GetPile(player).Cards.ToList();
        if (handCards.Count == 0)
            return;

        // 筛选攻击牌
        var attackCards = handCards.Where(c => c.Type == CardType.Attack).ToList();
        if (attackCards.Count == 0)
            return;

        Flash();

        // 随机选择一张攻击牌编织意志丝线
        var targetCard = attackCards[player.RunState.Rng.CombatTargets.NextInt(attackCards.Count)];
        var mindSilk = (MindSilkEnchantment)ModelDb.Enchantment<MindSilkEnchantment>().ToMutable();
        WeaveCmd.Weave(targetCard, mindSilk, mustReplace: false, canReplace: true);

        await Task.CompletedTask;
    }

    /// <summary>
    /// 修改伤害倍率：拥有丝线的卡牌造成的伤害提升33%
    /// 对应原版 atDamageModify：damage * (1 + triggerTimes/3)
    /// 
    /// 在STS2中，丝线通过 Enchantment 系统实现，没有 triggerTimes 概念。
    /// 简化实现：只要卡牌有丝线附魔，伤害提升33%。
    /// </summary>
    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        // 只影响玩家打出的伤害
        if (dealer?.Side != CombatSide.Player)
            return 1m;

        // 检查卡牌是否有丝线附魔
        if (cardSource?.Enchantment is AbstractSilkEnchantment)
        {
            return 1.33m; // 提升33%
        }

        return 1m;
    }
}
