using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 更迭
/// 1费普通技能
/// 将所有固有微尘移入弃牌堆。抽等同于消耗的微尘层数+X张牌（最多消耗5层）。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public class Iterate() : TheresaCardModel(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [DustKeyword.Dust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ExtraDraw", 1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(DustKeyword.Dust),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;

        // 1. 获取当前所有固有微尘（复制列表）
        var dustCards = DustManager.Cards.Where(c => c.Owner == Owner).ToList();

        // 2. 将所有固有微尘移入弃牌堆（带飞出动画）
        foreach (var card in dustCards)
        {
            if (!DustManager.Cards.Contains(card)) continue;
            
            // 播放飞出动画
            await DustManager.PlayCardFromDustAnimation(card);

            await DustManager.RemoveCard(card);
            await CardPileCmd.Add(card, PileType.Discard);
        }

        // 3. 获取 MantraPower 层数，计算实际消耗（最多5层）
        var mantraPower = Owner.Creature.GetPower<MantraPower>();
        int mantraStacks = mantraPower?.Amount ?? 0;
        int consumeCount = Math.Min(mantraStacks, 5);

        // 4. 消耗 MantraPower
        if (consumeCount > 0 && mantraPower != null)
        {
            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), mantraPower, -consumeCount, Owner.Creature, this);
        }

        // 5. 抽牌 = 消耗的微尘层数 + ExtraDraw
        // 让 DustDrawPatch 正常拦截转化，但根据实际到手数量补抽
        int drawCount = consumeCount + (int)DynamicVars["ExtraDraw"].BaseValue;
        if (drawCount > 0)
        {
            var hand = PileType.Hand.GetPile(Owner);
            int handCountBefore = hand?.Cards.Count ?? 0;
            int maxHandSize = 10;
            int effectiveDrawCount = Math.Min(drawCount, maxHandSize - handCountBefore);
            
            if (effectiveDrawCount > 0)
            {
                // 正常抽牌（DustDrawPatch 会拦截转化部分为微尘）
                await CardPileCmd.Draw(choiceContext, effectiveDrawCount, Owner);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ExtraDraw"].UpgradeValueBy(1m);
    }
}
