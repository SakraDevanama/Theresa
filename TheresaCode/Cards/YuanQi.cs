using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 缘起
/// 3费技能牌（升级后2费）
/// 抽取10张卡牌后丢弃7张，回复3点生命值。
/// 升级后：抽取10张卡牌后丢弃7张，回复4点生命值。
/// </summary>

[Pool(typeof(TheresaCardPool))]
public sealed class YuanQi : TheresaCardModel
{
    public YuanQi() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(10),
        new CardsVar("Discard", 7),
        new HealVar(3m)
    ];
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [DimKeyword.Dim];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 1. 抽取10张牌
        var drawnCards = (await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].BaseValue, Owner)).ToList();
        
        // 2. 如果抽到了牌，选择7张丢弃
        if (drawnCards.Count > 0)
        {
            int discardCount = (int)DynamicVars["Discard"].BaseValue;
            
            // 如果抽到的牌少于需要丢弃的数量，则全部丢弃
            if (drawnCards.Count <= discardCount)
            {
                foreach (var card in drawnCards)
                {
                    await CardCmd.Discard(choiceContext, card);
                }
            }
            else
            {
                // 让玩家选择要丢弃的牌
                var prefs = new CardSelectorPrefs(
                    new LocString("card_selection", "TO_DISCARD"),
                    discardCount,
                    discardCount
                );
                var cardsToDiscard = await CardSelectCmd.FromHandForDiscard(
                    choiceContext, 
                    Owner, 
                    prefs, 
                    null, 
                    this
                );
                
                foreach (var card in cardsToDiscard)
                {
                    await CardCmd.Discard(choiceContext, card);
                }
            }
        }

        // 3. 回复生命值
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        // 升级后费用-1
        EnergyCost.UpgradeBy(-1);
        // 升级后回复生命值+1
        DynamicVars["Heal"].UpgradeValueBy(1m);
    }
}
