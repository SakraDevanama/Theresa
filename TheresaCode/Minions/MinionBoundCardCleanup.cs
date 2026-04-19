using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Theresa.TheresaCode.Minions.Interfaces;

namespace Theresa.TheresaCode.Minions;

/// <summary>
/// 随从绑定卡牌清理工具
/// 当随从死亡时，直接从游戏中移除所有绑定到该随从的卡牌（不进入任何牌堆）
/// </summary>
public static class MinionBoundCardCleanup
{
    /// <summary>
    /// 为指定随从订阅死亡清理事件
    /// 在随从召唤时调用，随从死亡时会自动清理所有绑定卡牌
    /// </summary>
    public static void SubscribeDeathCleanup(Creature minion)
    {
        if (minion == null) return;
        
        // 使用局部变量捕获 minionCombatId，避免闭包引用已释放的对象
        var minionCombatId = minion.CombatId;
        
        // 对于随从(Pet)，Player 为 null，应该用 PetOwner
        var player = minion.Player ?? minion.PetOwner;
        
        if (player == null || minionCombatId == null)
        {
            MainFile.Logger?.Info($"[MinionBoundCardCleanup] Skip subscription: player={player != null}, combatId={minionCombatId != null}");
            return;
        }

        // 订阅 Died 事件 — 这是原版死亡流程中 BeforeDeath 之后、AfterDeath 之前的时机
        minion.Died += OnMinionDied;
        
        MainFile.Logger?.Info($"[MinionBoundCardCleanup] Subscribed death cleanup for minion {minion.Name} (CombatId={minionCombatId})");

        void OnMinionDied(Creature diedCreature)
        {
            try
            {
                // 取消订阅，避免重复触发（虽然 Died 事件通常只触发一次）
                diedCreature.Died -= OnMinionDied;
                
                MainFile.Logger?.Info($"[MinionBoundCardCleanup] Minion {diedCreature.Name} died, removing bound cards...");
                
                // 执行卡牌清理
                RemoveBoundCards(minionCombatId.Value, player);
            }
            catch (Exception ex)
            {
                MainFile.Logger?.Info($"[MinionBoundCardCleanup] Error during death cleanup: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 直接移除所有绑定到指定随从的卡牌（供 Power/Buff 在控制随从死亡时调用）
    /// </summary>
    public static void RemoveBoundCardsOnMinionDeath(Creature minion)
    {
        if (minion == null) return;
        
        var minionCombatId = minion.CombatId;
        var player = minion.Player ?? minion.PetOwner;
        
        if (player == null || minionCombatId == null)
        {
            MainFile.Logger?.Info($"[MinionBoundCardCleanup] Skip removal: player={player != null}, combatId={minionCombatId != null}");
            return;
        }

        MainFile.Logger?.Info($"[MinionBoundCardCleanup] Removing bound cards for minion {minion.Name} (CombatId={minionCombatId})");
        RemoveBoundCards(minionCombatId.Value, player);
    }

    /// <summary>
    /// 移除所有绑定到指定随从的卡牌（直接从游戏中移除，不进入任何牌堆）
    /// </summary>
    private static void RemoveBoundCards(uint minionCombatId, Player player)
    {
        var combatState = player.Creature.CombatState;
        if (combatState == null) return;

        var cardsToRemove = new List<CardModel>();

        // 收集所有牌堆中的绑定卡牌
        CollectBoundCards(PileType.Hand.GetPile(player), minionCombatId, cardsToRemove);
        CollectBoundCards(PileType.Draw.GetPile(player), minionCombatId, cardsToRemove);
        CollectBoundCards(PileType.Discard.GetPile(player), minionCombatId, cardsToRemove);
        CollectBoundCards(PileType.Play.GetPile(player), minionCombatId, cardsToRemove);

        if (cardsToRemove.Count == 0) return;

        MainFile.Logger?.Info($"[MinionBoundCardCleanup] Removing {cardsToRemove.Count} cards bound to minion {minionCombatId}");

        foreach (var card in cardsToRemove)
        {
            RemoveCardDirectly(card, player);
        }
    }

    /// <summary>
    /// 从指定牌堆中收集绑定到随从的卡牌
    /// </summary>
    private static void CollectBoundCards(CardPile pile, uint minionCombatId, List<CardModel> result)
    {
        if (pile == null) return;
        foreach (var card in pile.Cards.ToList())
        {
            if (IsBoundToMinion(card, minionCombatId))
                result.Add(card);
        }
    }

    /// <summary>
    /// 直接将卡牌从游戏中移除（不进入任何牌堆）
    /// </summary>
    private static void RemoveCardDirectly(CardModel card, Player player)
    {
        try
        {
            // 1. 如果卡牌在手牌中，先安全地移除视觉节点
            if (card.Pile?.Type == PileType.Hand)
            {
                RemoveHandCardVisuals(card);
            }

            // 2. 从当前牌堆移除
            card.RemoveFromCurrentPile();

            // 3. 从战斗状态中彻底移除
            var combatState = player.Creature.CombatState;
            if (combatState != null && combatState.ContainsCard(card))
            {
                combatState.RemoveCard(card);
            }

            // 4. 标记为已从状态中移除
            card.HasBeenRemovedFromState = true;

            MainFile.Logger?.Info($"[MinionBoundCardCleanup] Removed card {card.Id} bound to minion");
        }
        catch (Exception ex)
        {
            MainFile.Logger?.Info($"[MinionBoundCardCleanup] Error removing card {card.Id}: {ex.Message}");
        }
    }

    /// <summary>
    /// 安全地移除手牌中卡牌的视觉节点
    /// </summary>
    private static void RemoveHandCardVisuals(CardModel card)
    {
        try
        {
            var hand = NPlayerHand.Instance;
            if (hand == null) return;

            // 检查卡牌是否真的有对应的 holder（避免抛异常）
            var holder = hand.GetCardHolder(card);
            if (holder != null)
            {
                hand.RemoveCardHolder(holder);
                MainFile.Logger?.Info($"[MinionBoundCardCleanup] Removed hand card visuals for {card.Id}");
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger?.Info($"[MinionBoundCardCleanup] Error removing hand card visuals: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查卡牌是否绑定到指定随从
    /// </summary>
    private static bool IsBoundToMinion(CardModel card, uint minionCombatId)
    {
        if (card is not IMinionBoundCard boundCard) return false;
        return boundCard.BoundMinionCombatId.HasValue && boundCard.BoundMinionCombatId.Value == minionCombatId;
    }
}
