using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 落幕
/// 1费技能牌
/// 获得14点格挡
/// 向手中放入一张晕眩
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class FinalCurtain() : TheresaCardModel(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromCard<Dazed>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(14m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 向手中放入一张晕眩（原版晕眩卡）
        if (CombatState != null)
        {
            var dazedCard = CombatState.CreateCard<Dazed>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(dazedCard, PileType.Hand, true);
        }
    }

    protected override void OnUpgrade()
    {

        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
