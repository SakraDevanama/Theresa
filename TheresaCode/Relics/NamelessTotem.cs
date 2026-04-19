using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Cards;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Relics;

/// <summary>
/// 无名图腾
/// 拾起时，从牌组中选择3张牌移除。
/// 每移除一张攻击牌，向牌组加入一张萨卡兹见证；
/// 每移除一张技能牌，向牌组加入一张萨卡兹叙说。
/// </summary>
[Pool(typeof(TheresaRelicPool))]
public sealed class NamelessTotem : TheresaRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    private const int SelectCount = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("SelectCount", SelectCount)
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner == null) return;

        var deckCards = Owner.Deck.Cards.ToList();
        if (deckCards.Count == 0) return;

        var maxSelect = Math.Min(SelectCount, deckCards.Count);
        var prefs = new CardSelectorPrefs(
            new LocString("static_hover_tips", "nameless_totem_choose_remove"),
            maxSelect,
            maxSelect
        )
        {
            Cancelable = false
        };

        var selectedCards = (await CardSelectCmd.FromSimpleGrid(
            new BlockingPlayerChoiceContext(),
            deckCards,
            Owner,
            prefs
        )).ToList();

        foreach (var card in selectedCards)
        {
            // 从牌组中移除
            await CardPileCmd.RemoveFromDeck(card);

            // 根据卡牌类型加入对应卡牌
            if (card.Type == CardType.Attack)
            {
                var newCard = Owner.RunState.CreateCard(ModelDb.Card<SarkazSee>(), Owner);
                await CardPileCmd.Add(newCard, PileType.Deck, CardPilePosition.Bottom, this);
            }
            else if (card.Type == CardType.Skill)
            {
                var newCard = Owner.RunState.CreateCard(ModelDb.Card<Astory>(), Owner);
                await CardPileCmd.Add(newCard, PileType.Deck, CardPilePosition.Bottom, this);
            }
        }

        Flash();
    }
}
