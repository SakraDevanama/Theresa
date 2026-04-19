using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;


[Pool(typeof(TheresaCardPool))]
public sealed class StoryChapterSeven() : TheresaCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MantraPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获取弃牌堆中的非状态牌
        var discardPile = PileType.Discard.GetPile(Owner);
        var nonStatusCards = discardPile.Cards.Where(c => c.Type != CardType.Status).ToList();
        
        // 获取手牌数量（此牌打出后已在打出堆，不在手牌中）
        var handPile = PileType.Hand.GetPile(Owner);
        var handCardsCount = handPile.Cards.Count;

        // 如果弃牌堆有非状态牌且手牌中有牌可以弃
        if (nonStatusCards.Count > 0 && handCardsCount > 0)
        {
            // 让玩家选择至多2张非状态牌
            var prefs = new CardSelectorPrefs(
                CardSelectorPrefs.DiscardSelectionPrompt,
                1,
                2
            )
            {
                Cancelable = true
            };

            var selectedCard = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                nonStatusCards,
                Owner,
                prefs
            )).FirstOrDefault();

            if (selectedCard != null)
            {
                // 丢弃至多5张手牌
                var discardPrefs = new CardSelectorPrefs(
                    CardSelectorPrefs.DiscardSelectionPrompt,
                    1,
                    5
                );

                var cardToDiscard = (await CardSelectCmd.FromHandForDiscard(
                    choiceContext,
                    Owner,
                    discardPrefs,
                    null,
                    this
                )).FirstOrDefault();

                if (cardToDiscard != null)
                {
                    await CardCmd.Discard(choiceContext, cardToDiscard);
                }

                // 将选择的牌放到抽牌堆顶
                await CardPileCmd.Add(selectedCard, PileType.Draw, CardPilePosition.Top);
            }
        }

        // 给予MantraPower
        await PowerCmd.Apply<MantraPower>(
            Owner.Creature,
            DynamicVars["MantraPower"].BaseValue,
            Owner.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MantraPower"].UpgradeValueBy(1);
    }
}
