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

/// <summary>
/// 自过去呼唤未来
/// 1费技能牌
/// 丢弃所有手牌，抽等量+1（+1）的牌。若无牌可丢则获得6层微尘。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class EchoFromPast() : TheresaCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromPower<MantraPower>(),
        HoverTipFactory.FromPower<DivinityStance>(),
    ];
    
    // 额外抽牌数
    private const int BaseExtraDraw = 1;
    private const int UpgradeExtraDrawBonus = 1;
    // 无牌可丢时获得的MantraPower层数
    private const int NoDiscardMantra = 6;

    // 添加消耗关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, LingerKeyword.Linger, DimKeyword.Dim];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ExtraDraw", BaseExtraDraw)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 获取当前手牌（此牌打出后已在打出堆，不在手牌中）
        var handPile = PileType.Hand.GetPile(Owner);
        var handCards = handPile.Cards.ToList();

        if (handCards.Count > 0)
        {
            // 丢弃所有手牌
            await CardCmd.Discard(choiceContext, handCards);

            // 抽等量 + 额外抽牌数 的牌
            var drawCount = handCards.Count + (int)DynamicVars["ExtraDraw"].BaseValue;
            await CardPileCmd.Draw(choiceContext, drawCount, Owner);
        }
        else
        {
            // 若无牌可丢，获得6层MantraPower
            await PowerCmd.Apply<MantraPower>(Owner.Creature, NoDiscardMantra, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ExtraDraw"].UpgradeValueBy(UpgradeExtraDrawBonus);
    }
}
