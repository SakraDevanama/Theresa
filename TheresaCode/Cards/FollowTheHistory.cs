using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 循历史而去 (FollowTheHistory)
/// 1费能力牌（升级后0费）
/// 每打出一张消耗牌，就给予自身2（+1）层MantraPower。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class FollowTheHistory() : TheresaCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [LingerKeyword.Linger];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MantraPower>(),
        HoverTipFactory.FromPower<DivinityStance>(),
    ];
    
    
    // 基础获得的MantraPower层数
    private const int BaseMantraAmount = 1;
    // 升级后增加的层数
    private const int UpgradeMantraBonus = 1;

    // 定义自定义变量，用于在卡面上显示数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MantraAmount", BaseMantraAmount)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 应用能力效果
        var mantraAmount = (int)DynamicVars["MantraAmount"].BaseValue;
        await PowerCmd.Apply<FollowTheHistoryEffect>(Owner.Creature, mantraAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级后费用变为0
        EnergyCost.UpgradeBy(-1);
        // 升级后Mantra层数+1
        DynamicVars["MantraAmount"].UpgradeValueBy(UpgradeMantraBonus);
    }
}

/// <summary>
/// 循历史而去效果
/// 每消耗一张牌，给予自身MantraPower
/// </summary>
public sealed class FollowTheHistoryEffect : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 提供 Amount 变量供 localization 使用
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Amount", Amount)
    ];

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        await base.AfterCardExhausted(choiceContext, card, causedByEthereal);
        
        // 确保拥有者存活
        if (Owner == null || !Owner.IsAlive) return;

        // 关键检查：只处理自己消耗的卡牌
        if (card.Owner != Owner.Player)
        {
            return;
        }

        // 获取Mantra层数（存储在能力的 Amount 中）
        int mantraAmount = (int)this.Amount;
        if (mantraAmount <= 0) mantraAmount = 2;

        // 异步应用Mantra能力
        _ = ApplyMantraAsync(mantraAmount);
    }

    /// <summary>
    /// 异步应用Mantra能力
    /// </summary>
    private async Task ApplyMantraAsync(int amount)
    {
        if (Owner == null) return;
        
        await PowerCmd.Apply<MantraPower>(Owner, amount, Owner, null);
    }
}