using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;


[Pool(typeof(TheresaCardPool))]
public sealed class HeroAndKing() : TheresaCardModel(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromPower<HeroesAndOverlordsPower>(),
        HoverTipFactory.FromPower<TheresiasHopePower>(),
        HoverTipFactory.FromPower<ZaakathHatePower>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 应用英雄与魔王Power（持续整个战斗）
        await PowerCmd.Apply<HeroesAndOverlordsPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); 
    }
}
