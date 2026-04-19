using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Keywords;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 在回忆中坠落
/// 0费技能牌
/// 普通
/// 萦绕1次。增加1点耗能。
/// 萦绕：降低2（+1）点耗能并放入手中。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class FallFromMemory : TheresaCardModel, IDustCard
{
    private const int CostReductionBase = 2;

    public FallFromMemory() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        LingerKeyword.Linger,
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("CostReduction", CostReductionBase)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(LingerKeyword.Linger),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 萦绕1次
        await DustManager.DustIt(false, false);

        // 增加1点耗能
        EnergyCost.UpgradeBy(1);
    }

    /// <summary>
    /// 被萦绕时：从微尘移到手牌，降低耗能
    /// </summary>
    public async Task<bool> TriggerWhenLingered()
    {
        if (Owner == null) return false;

        // 从 DustManager 移除
        await DustManager.RemoveCard(this);

        // 移入手牌
        await CardPileCmd.Add(this, PileType.Hand);

        // 降低耗能
        int reduction = (int)DynamicVars["CostReduction"].BaseValue;
        EnergyCost.UpgradeBy(-reduction);

        return true; // 自己处理了萦绕逻辑
    }

    protected override void OnUpgrade()
    {
        DynamicVars["CostReduction"].UpgradeValueBy(1m);
    }
}
