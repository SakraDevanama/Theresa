using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using CardModel = MegaCrit.Sts2.Core.Models.CardModel;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Cards;

[Pool(typeof(TheresaCardPool))]
public sealed class SadTime() : TheresaCardModel(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        int amount = DynamicVars["Amount"].IntValue;
        var hand = PileType.Hand.GetPile(Owner);
        var drawPile = PileType.Draw.GetPile(Owner);
        var discardPile = PileType.Discard.GetPile(Owner);

        var typeCounts = new Dictionary<CardType, int>
        {
            { CardType.Attack, 0 },
            { CardType.Skill, 0 },
            { CardType.Power, 0 },
            { CardType.Status, 0 },
            { CardType.Curse, 0 },
        };

        foreach (var card in hand.Cards)
        {
            if (typeCounts.ContainsKey(card.Type))
                typeCounts[card.Type]++;
        }

        var typeOrder = new[]
        {
            CardType.Attack,
            CardType.Skill,
            CardType.Power,
            CardType.Status,
            CardType.Curse,
        };

        foreach (var type in typeOrder)
        {
            int deficit = amount - typeCounts[type];
            if (deficit <= 0) continue;

            var matchingCards = new List<CardModel>();
            var drawCards = drawPile.Cards.ToList();

            foreach (var card in drawCards)
            {
                if (matchingCards.Count >= deficit) break;
                if (card.Type == type)
                    matchingCards.Add(card);
            }

            if (matchingCards.Count < deficit)
            {
                var discardMatches = discardPile.Cards
                    .Where(c => c.Type == type)
                    .ToList();

                if (discardMatches.Count > 1)
                {
                    discardMatches.StableShuffle(Owner.RunState.Rng.Shuffle);
                }

                int remains = deficit - matchingCards.Count;
                for (int i = 0; i < remains && i < discardMatches.Count; i++)
                {
                    matchingCards.Add(discardMatches[i]);
                }
            }

            if (matchingCards.Count > 0)
            {
                var reversed = matchingCards.ToList();
                reversed.Reverse();

                foreach (var card in reversed)
                {
                    if (discardPile.Cards.Contains(card))
                    {
                        await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
                    }
                    else if (drawPile.Cards.Contains(card))
                    {
                        await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
                    }
                }

                await CardPileCmd.Draw(choiceContext, matchingCards.Count, Owner);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(1);
    }
}
