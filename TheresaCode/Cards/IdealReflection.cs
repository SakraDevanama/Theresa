using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 理想的倒影
/// 1费技能牌
/// 获得9点格挡。从弃牌堆中选择一张牌，将其复制加入手牌，本回合可以免费打出。该复制牌会被消耗。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class IdealReflection() : TheresaCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2. 获取弃牌堆中的卡牌
        var discardPile = PileType.Discard.GetPile(Owner);
        var discardCards = discardPile.Cards.ToList();

        if (discardCards.Count == 0) return;

        // 3. 让玩家选择一张卡牌
        var selectionPrompt = new LocString("static_hover_tips", "choose_a_card_from_discard");
        var prefs = new CardSelectorPrefs(
            selectionPrompt,
            1,
            1
        )
        {
            Cancelable = false
        };

        var selectedCardModels = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            discardCards,
            Owner,
            prefs
        )).ToList();

        if (!selectedCardModels.Any()) return;

        var selectedCard = selectedCardModels.First();

        // 4. 复制这张牌
        var copiedCard = selectedCard.CreateClone();

        // 5. 添加消耗关键词
        copiedCard.AddKeyword(CardKeyword.Exhaust);

        // 6. 本回合免费打出
        copiedCard.SetToFreeThisTurn();

        // 7. 加入手牌
        await CardPileCmd.Add(copiedCard, PileType.Hand);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
