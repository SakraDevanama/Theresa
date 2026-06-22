using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Keywords;

namespace Theresa.TheresaCode.Enchantments;

/// <summary>
/// 愿景丝线附魔
/// 
/// 效果：
/// 1. 仅当全牌组中愿景少于3张时可以附魔
/// 2. 仅附魔给可以转化为微尘的牌（攻击/技能）
/// 3. 抽到时：若微尘中没有此牌的愿景副本，可超出上限地转化为微尘
/// 4. 回合结束时（在微尘中）：萦绕1次并将此牌丢弃
/// </summary>
public class WishSilkEnchantment : CustomEnchantmentModel
{
    protected override string? CustomIconPath => "res://Theresa/images/icons/silk_thread3.png";
    public override bool ShowAmount => true;

    protected override void OnEnchant()
    {
        // 注册卡牌抽到时的事件
        if (Card != null)
        {
            Card.Drawn += OnCardDrawn;
        }
    }

    /// <summary>
    /// 卡牌被抽到手中时触发：检查是否转化为微尘。
    /// 为避免联机时延迟导致 host/client 状态分歧，不再等待抽牌动画，直接执行。
    /// </summary>
    private void OnCardDrawn()
    {
        if (Card == null) return;
        if (Card.Owner == null) return;

        _ = ConvertToDustAsync();
    }

    private async Task ConvertToDustAsync()
    {
        if (Card == null) return;
        if (Card.Owner == null) return;

        // 防御性检查：Dim 牌不应被转化为微尘
        if (Card.Keywords.Contains(Theresa.TheresaCode.Keywords.DimKeyword.Dim))
        {
            MainFile.Logger?.Info($"[WishSilkEnchantment] Dim card {Card.Id.Entry} drew with Wish; ignoring dust conversion");
            return;
        }

        // 检查卡牌是否还在手牌中
        var hand = PileType.Hand.GetPile(Card.Owner);
        if (hand == null || !hand.Cards.Contains(Card)) return;

        // 检查微尘中是否已有此牌的愿景副本
        var hasCopyInDust = DustManager.CardsFor(Card.Owner).Any(c =>
            c.Enchantment is WishSilkEnchantment &&
            c.Id == Card.Id &&
            c != Card);

        if (!hasCopyInDust)
        {
            // 清理手牌中的视觉节点
            var handNode = NCombatRoom.Instance?.Ui?.Hand;
            if (handNode != null)
            {
                var holder = handNode.GetCardHolder(Card);
                if (holder != null)
                {
                    handNode.RemoveCardHolder(holder);
                }
            }

            // 从当前牌堆移除此牌
            Card.RemoveFromCurrentPile();
            // 放入微尘（可超出上限）
            await DustManager.AddCardOverLimit(Card);
        }
    }

    /// <summary>
    /// 检查是否可以附魔：仅攻击/技能牌，且全牌组愿景少于3张
    /// </summary>
    public override bool CanEnchant(CardModel card)
    {
        if (!base.CanEnchant(card)) return false;

        // Dim 牌不会进入微尘，因此不能承载“愿景”效果
        if (card.Keywords.Contains(Theresa.TheresaCode.Keywords.DimKeyword.Dim))
            return false;

        // 仅攻击/技能牌可以转化为微尘
        if (card.Type != CardType.Attack && card.Type != CardType.Skill)
            return false;
        
        // 检查全牌组中愿景数量
        if (card.Owner == null) return false;
        
        int wishCount = 0;
        const int maxWish = 3;
        
        // 统计所有牌堆中的愿景
        foreach (var pile in card.Owner.Piles)
        {
            foreach (var c in pile.Cards)
            {
                if (c.Enchantment is WishSilkEnchantment)
                    wishCount++;
            }
        }
        
        // 统计微尘中的愿景
        foreach (var c in DustManager.CardsFor(card.Owner))
        {
            if (c.Enchantment is WishSilkEnchantment)
                wishCount++;
        }
        
        if (wishCount >= maxWish)
            return false;
        
        return true;
    }
}
