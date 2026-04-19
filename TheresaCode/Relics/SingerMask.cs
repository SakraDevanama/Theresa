using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Commands;
using Theresa.TheresaCode.Enchantments;

namespace Theresa.TheresaCode.Relics;

/// <summary>
/// 讴歌者面纱 (SingerMask)
/// 罕见遗物
/// 
/// 效果：
/// 1. 你的非攻击伤害提升2点。
/// 2. 战斗开始时，使随机手牌编织：泪水。
/// 3. 每当你打出能力牌时，使随机手牌编织：泪水。
/// 
/// Java 原版：
/// - atPreBattle: 重置 triggered 标志
/// - atTurnStartPostDraw: 第一次回合开始时对手牌随机卡牌编织泪水
/// - onUseCard(POWER): 打出能力牌时对手牌随机卡牌编织泪水
/// - DamagePatch: 非攻击伤害+2
/// </summary>
[Pool(typeof(TheresaRelicPool))]
public sealed class SingerMask : TheresaRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    // 标记本战斗是否已触发过战斗开始时的编织效果
    private bool _combatStartWeaveTriggered;

    /// <summary>
    /// 战斗开始时重置标记
    /// </summary>
    public override Task BeforeCombatStart()
    {
        _combatStartWeaveTriggered = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 玩家回合开始后（抽牌完成后）触发
    /// 第一次回合开始时，对手牌随机卡牌编织泪水丝线
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner != player) return;
        if (_combatStartWeaveTriggered) return;

        _combatStartWeaveTriggered = true;

        var handCards = PileType.Hand.GetPile(player)?.Cards;
        if (handCards == null || handCards.Count == 0)
            return;

        Flash();

        // 对手牌随机卡牌编织泪水丝线
        var tearSilk = (TearSilkEnchantment)ModelDb.Enchantment<TearSilkEnchantment>().ToMutable();
        WeaveCmd.WeaveRandom(handCards, tearSilk, mustReplace: false, canReplace: true);

        MainFile.Logger?.Info($"[SingerMask] Combat start weave triggered on random hand card");

        await Task.CompletedTask;
    }

    /// <summary>
    /// 卡牌打出后触发
    /// 如果打出的是能力牌，对手牌随机卡牌编织泪水丝线
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;
        if (cardPlay.Card?.Type != CardType.Power) return;

        var handCards = PileType.Hand.GetPile(Owner)?.Cards;
        if (handCards == null || handCards.Count == 0)
            return;

        Flash();

        // 对手牌随机卡牌编织泪水丝线
        var tearSilk = (TearSilkEnchantment)ModelDb.Enchantment<TearSilkEnchantment>().ToMutable();
        WeaveCmd.WeaveRandom(handCards, tearSilk, mustReplace: false, canReplace: true);

        MainFile.Logger?.Info($"[SingerMask] Power card played, weave triggered on random hand card");

        await Task.CompletedTask;
    }

    /// <summary>
    /// 非攻击伤害+2
    /// 对应原版 DamagePatch：info.type != DamageType.NORMAL 时伤害+2
    /// 
    /// 在STS2中，非攻击伤害通过 ValueProp 判断：
    /// - 攻击伤害通常带有 ValueProp.Move
    /// - 非攻击伤害（Power效果、荆棘等）不带 ValueProp.Move
    /// </summary>
    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        // 只影响玩家造成的伤害
        if (dealer?.Side != CombatSide.Player)
            return 0m;

        // 只影响非攻击伤害（不带 Move 标记的伤害）
        if (props.HasFlag(ValueProp.Move))
            return 0m;

        // 非攻击伤害+2
        Flash();
        return 2m;
    }
}
