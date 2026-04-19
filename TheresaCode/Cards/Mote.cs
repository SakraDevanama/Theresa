using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Cards;




[Pool(typeof(TheresaCardPool))]


public sealed class Mote() : TheresaCardModel(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<MantraPower>(),
        HoverTipFactory.FromPower<DivinityStance>(),
    ];

    public override CardPoolModel Pool => ModelDb.CardPool<TheresaCardPool>();
    
    protected override bool IsPlayable =>
        EnergyCost.GetResolved() <= 0 || Owner.Creature.Powers.OfType<MantraPower>().Any(p => p.Amount > 0);
   
    protected override IEnumerable<DynamicVar> CanonicalVars => [ new CardsVar(2), new EnergyVar(1)];

    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner.Creature;
       
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await PowerCmd.Apply<MantraPower>(new ThrowingPlayerChoiceContext(), owner, -1m, owner, this);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }


    protected override void OnUpgrade()
    {

        AddKeyword(CardKeyword.Innate);
    }
}