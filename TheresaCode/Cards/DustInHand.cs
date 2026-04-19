using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Powers;
using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 执尘
/// 1费技能牌
/// 抽2（+1）张牌
/// 选择至多8张牌（至少0张牌）丢弃并转化为微尘
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class DustInHand() : TheresaCardModel(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [LingerKeyword.Linger];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MantraPower>(),
        HoverTipFactory.FromPower<DivinityStance>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 1. 抽牌
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);

        // 2. 选择至多8张牌（至少0张）丢弃
        var discardPrefs = new CardSelectorPrefs(
            CardSelectorPrefs.DiscardSelectionPrompt,
            0, // 最少0张
            8  // 最多8张
        )
        {
            Cancelable = true
        };

        var cardsToDiscard = await CardSelectCmd.FromHandForDiscard(
            choiceContext,
            Owner,
            discardPrefs,
            null,
            this
        );

        // 3. 丢弃并转化为微尘（获得MantraPower）
        if (cardsToDiscard.Any())
        {
            await CardCmd.Discard(choiceContext, cardsToDiscard);
            // 每丢弃一张牌获得1层MantraPower（转化为微尘）
            await PowerCmd.Apply<MantraPower>(
                Owner.Creature,
                cardsToDiscard.Count(),
                Owner.Creature,
                this
            );
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后抽牌数 +1（从2变为3）
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
