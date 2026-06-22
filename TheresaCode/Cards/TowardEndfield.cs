using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.TestSupport;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Keywords;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 驶向终端 (TowardEndfield) 
/// 0费技能牌，普通稀有度
/// 
/// 效果：将1张微尘移回手牌。弃置 !M! 张手牌，被弃置的牌转化为微尘。
/// 升级：弃置数量+1
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class TowardEndfield() : TheresaCardModel(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // MagicNumber = 弃牌数量，基础1，升级后2
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [DimKeyword.Dim];
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 将最旧的微尘（列表第一个）移回手牌
        await MoveOldestDustToHand(choiceContext);

        // 2. 弃置指定数量的手牌，能变微尘的变微尘，不能变的进弃牌堆
        int discardCount = DynamicVars.Cards.IntValue;
        await DiscardHandToDust(choiceContext, discardCount);
    }

    /// <summary>
    /// 将最旧的微尘移回手牌
    /// </summary>
    private async Task MoveOldestDustToHand(PlayerChoiceContext choiceContext)
    {
        if (Owner == null) return;
        var dustCards = DustManager.CardsFor(Owner).ToList();
        if (dustCards.Count == 0) return;

        // 取最旧的微尘（列表第一个，即最早添加的）
        var oldestDust = dustCards[0];
        if (oldestDust.Owner == null) return;

        // 从微尘移除
        await DustManager.RemoveCard(oldestDust);

        // 移回手牌（如果手牌满则进弃牌堆）
        var hand = PileType.Hand.GetPile(oldestDust.Owner);
        if (hand.Cards.Count >= 10)
        {
            await CardPileCmd.Add(oldestDust, PileType.Discard);
        }
        else
        {
            await CardPileCmd.Add(oldestDust, PileType.Hand);
        }
    }

    /// <summary>
    /// 弃置手牌，能变微尘的攻击/技能牌转化为微尘，其他进弃牌堆
    /// </summary>
    private async Task DiscardHandToDust(PlayerChoiceContext choiceContext, int amount)
    {
        if (Owner == null) return;

        var hand = PileType.Hand.GetPile(Owner);
        var handCards = hand.Cards.ToList();

        if (handCards.Count == 0) return;

        // 如果手牌数量 <= 需要弃置的数量，全部弃置（无需选择）
        if (handCards.Count <= amount)
        {
            foreach (var card in handCards)
            {
                await ProcessDiscardToDust(card);
            }
            return;
        }

        // 需要玩家选择弃置的牌
        var selected = await CardSelectCmd.FromHandForDiscard(
            choiceContext,
            Owner,
            new CardSelectorPrefs(
                CardSelectorPrefs.DiscardSelectionPrompt,
                amount,
                amount
            ),
            null,
            this
        );

        foreach (var card in selected)
        {
            await ProcessDiscardToDust(card);
        }
    }

    /// <summary>
    /// 处理单张牌的弃置：能变微尘的攻击/技能变微尘，其他进弃牌堆
    /// </summary>
    private static async Task ProcessDiscardToDust(CardModel card)
    {
        if (card.Owner == null) return;

        // 检查是否能变微尘：攻击或技能牌、非 Dim、且微尘未达上限
        bool canBecomeDust = (card.Type == CardType.Attack || card.Type == CardType.Skill)
            && !card.Keywords.Contains(DimKeyword.Dim)
            && !DustManager.IsFull(card.Owner);

        if (canBecomeDust)
        {
            // 先通过官方 CardPileCmd 把卡牌从手牌移入弃牌堆并播放标准动画。
            // 这一步至关重要：CardSelectCmd 返回的卡牌仍处于手牌视图的可交互状态，
            // 直接调用 DustManager.AddCard 会在"选择窗口已关闭、Dust 动画尚未开始"的
            // 时间窗口内留下可被点击的卡牌，导致玩家误打出已进入微尘流程的牌。
            await CardPileCmd.Add(card, PileType.Discard);

            // 再将其从弃牌堆转换到微尘。
            await DustManager.AddCard(card);
        }
        else
        {
            // 正常弃置到弃牌堆
            await CardPileCmd.Add(card, PileType.Discard);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级：弃牌数量+1
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
