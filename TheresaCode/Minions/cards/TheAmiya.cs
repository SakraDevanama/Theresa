using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
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
/// 约定：阿米娅 (TheAmiya)
/// 0费能力牌
/// 固有 消耗
/// 消耗5个微尘。召唤阿米娅。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class TheAmiya() : TheresaCardModel(0, CardType.Quest, CardRarity.Event, TargetType.Self)
{
    private const int DustCost = 5;

    // 固有 + 消耗 + 召唤阿米娅关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Retain,
        CardKeyword.Exhaust,
        SummonAmiyaKeyword.SummonAmiya,
        LingerKeyword.Linger,
        DimKeyword.Dim
    ];

    // 提示文本：5个微尘   
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<MantraPower>(),
        HoverTipFactory.FromPower<AmiyaAuraPower>(),
        HoverTipFactory.FromPower<AmiyaCrescendoAction>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DustCost", DustCost),
        new DynamicVar("AmiyaAuraPower", 4)  // 阿米娅光环持续回合数
    ];

    /// <summary>
    /// 检查是否可打出：需要至少5个微尘
    /// </summary>
    protected override bool IsPlayable =>
        Owner?.Creature?.Powers.OfType<MantraPower>().Sum(p => (int)p.Amount) >= DustCost;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner?.Creature;
        if (owner == null) return;

        // 消耗5个微尘
        var mantraPower = owner.Powers.OfType<MantraPower>().FirstOrDefault();
        if (mantraPower != null)
        {
            await PowerCmd.Apply<MantraPower>(new ThrowingPlayerChoiceContext(), owner, -DustCost, owner, this);
        }

        // 召唤阿米娅
        _ = await MinionCmd.AddMinion<AmiyaMinion>(Owner, new MinionSummonOptions(
            20m,  // 基础生命
            2m,   // 基础力量
            Source: this,
            Position: MinionPosition.Front));
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
