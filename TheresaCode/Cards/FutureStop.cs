using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using CardModel = MegaCrit.Sts2.Core.Models.CardModel;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Enchantments;

namespace Theresa.TheresaCode.Cards;

[Pool(typeof(TheresaCardPool))]
public sealed class FutureStop() : TheresaCardModel(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        var hand = PileType.Hand.GetPile(Owner);
        if (hand == null) return;

        var handCards = hand.Cards.ToList();
        int stopCount = Math.Max(0, handCards.Count - 1);

        for (int i = 0; i < stopCount; i++)
        {
            if (hand.Cards.Count >= 10) break;

            var stopCard = CombatState?.CreateCard<Stop>(Owner)
                ?? Owner.RunState.CreateCard<Stop>(Owner);

            var cocoonSilk = new CocoonSilkEnchantment();
            CardCmd.Enchant(cocoonSilk, stopCard, cocoonSilk.Amount);

            await CardPileCmd.AddGeneratedCardToCombat(stopCard, PileType.Hand, Owner);
        }

        if (!cardPlay.IsAutoPlay)
        {
            CombatManager.Instance?.SetReadyToEndTurn(Owner, canBackOut: false);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
