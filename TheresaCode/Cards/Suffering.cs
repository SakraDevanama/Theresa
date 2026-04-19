using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 苦难
/// X费技能牌（消耗）
/// 获得X+1层希望和恨意
/// 升级后获得X+2层
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class Suffering() : TheresaCardModel(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [

        HoverTipFactory.FromPower<TheresiasHopePower>(),
        HoverTipFactory.FromPower<ZaakathHatePower>()
    ];
    

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<TheresiasHopePower>(2m),
        new PowerVar<ZaakathHatePower>(2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        int xValue = ResolveEnergyXValue();
        int hopeAmount = xValue + (int)DynamicVars["TheresiasHopePower"].BaseValue;
        int hateAmount = xValue + (int)DynamicVars["ZaakathHatePower"].BaseValue;

        await PowerCmd.Apply<TheresiasHopePower>(new ThrowingPlayerChoiceContext(), Owner.Creature, hopeAmount, Owner.Creature, this);
        await PowerCmd.Apply<ZaakathHatePower>(new ThrowingPlayerChoiceContext(), Owner.Creature, hateAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["TheresiasHopePower"].UpgradeValueBy(3m);
        DynamicVars["ZaakathHatePower"].UpgradeValueBy(3m);
    }
}
