using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Commands;
using MinionLib.Minion;
using Theresa.TheresaCode.Cards;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Minions.Models;
using Theresa.TheresaCode.Minions.Powers;
using Theresa.TheresaCode.Powers;
using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Minions.Cards;

/// <summary>
/// 约誓：特雷西斯 (TheSwordsman)
/// 2费能力牌
/// 消耗3点微尘。召唤特雷西斯。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class TheSwordsman() : TheresaCardModel(3, CardType.Quest, CardRarity.Event, TargetType.Self)
{
    private const int DustCost = 2;
    private const int BaseHp = 10;
    private const int UpgradedHp = 30;

    // 召唤特雷西斯关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        SummonSwordsmanKeyword.SummonSwordsman,
        CardKeyword.Retain,
        CardKeyword.Exhaust,
        LingerKeyword.Linger,
        DimKeyword.Dim,
    ];

    // 提示文本：微尘
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<MantraPower>(),
        HoverTipFactory.FromCard<GuardianSlashBound>(),
        HoverTipFactory.FromPower<SwordsmanSlashAction>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DustCost", DustCost)
    ];

    /// <summary>
    /// 检查是否可打出：需要至少2个微尘
    /// </summary>
    protected override bool IsPlayable =>
        Owner?.Creature?.Powers.OfType<MantraPower>().Sum(p => (int)p.Amount) >= DustCost;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner?.Creature;
        if (owner == null) return;

        // 消耗3个微尘
        var mantraPower = owner.Powers.OfType<MantraPower>().FirstOrDefault();
        if (mantraPower != null)
        {
            await PowerCmd.Apply<MantraPower>(owner, -DustCost, owner, this);
        }

        // 召唤特雷西斯（升级后30生命，未升级25生命）
        var hp = IsUpgraded ? UpgradedHp : BaseHp;
        _ = await MinionCmd.AddMinion<SwordsmanMinion>(Owner, new MinionSummonOptions(
            hp,   // 生命
            5m,   // 基础力量
            Source: this,
            Position: MinionPosition.Front));
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
