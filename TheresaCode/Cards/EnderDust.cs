using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 末影尘埃 (EnderDust)
/// 2费技能牌，普通稀有度，消耗
/// 
/// 效果：获得1层末影。
/// 抽到手中时：丢弃并获得5点格挡。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class EnderDust() : TheresaCardModel(2, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<EndShadowPower>()];

    // 格挡数值：抽到手中时获得的基础格挡
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, ValueProp.Move)];

    /// <summary>
    /// 打出时：获得1层末影
    /// </summary>
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<EndShadowPower>(new ThrowingPlayerChoiceContext(), 
            Owner.Creature,
            1,
            Owner.Creature,
            this
        );
    }

    /// <summary>
    /// 抽到手中时：丢弃并获得格挡
    /// </summary>
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;
        if (CombatState == null) return;

        // 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);

        // 丢弃此牌（不是消耗，是丢弃到弃牌堆）
        await CardPileCmd.Add(this, PileType.Discard);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
