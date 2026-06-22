using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using CardModel = MegaCrit.Sts2.Core.Models.CardModel;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;

namespace Theresa.TheresaCode.Cards;

[Pool(typeof(TheresaCardPool))]
public sealed class Stop() : TheresaCardModel(-2, CardType.Status, CardRarity.Token, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("SilkTriggers", 2m)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [SilkKeyword.Silk, CardKeyword.Unplayable];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
    }

    protected override void OnUpgrade()
    {
        DynamicVars["SilkTriggers"].UpgradeValueBy(1);
    }
}
