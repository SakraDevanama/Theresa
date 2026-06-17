using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Theresa.TheresaCode.Keywords;

namespace Theresa.TheresaCode.Utils;

/// <summary>
/// 重现功能帮助类
/// 处理"重现"关键词的核心逻辑：检视本局游戏从牌组移除的卡牌，选择复制到手牌
/// </summary>
public static class ReplayHelper
{
    /// <summary>
    /// 执行重现效果
    /// 检视本局游戏从牌组移除过的卡牌，选择1张复制到手中并添加消耗属性，使其本回合耗能-1。
    /// 每张牌每场战斗只能重现1次。
    /// </summary>
    /// <param name="context">玩家选择上下文</param>
    /// <param name="sourceCard">触发重现的源卡牌</param>
    /// <param name="combatState">战斗状态</param>
    /// <param name="count">重现的卡牌数量</param>
    /// <param name="upgradeForRun">是否使重现的卡牌在整局游戏中升级</param>
    public static async Task ExecuteReplay(
        PlayerChoiceContext context,
        CardModel sourceCard,
        CombatState combatState,
        int count = 1,
        bool upgradeForRun = false)
    {
        // 【关键限制】检查源卡牌是否已重现过
        if (sourceCard.HasBeenReplayedThisCombat())
        {
            MainFile.Logger?.Info($"[ReplayHelper] Source card already replayed this combat, skipping");
            return;
        }

        // 标记源卡牌为已重现
        sourceCard.MarkReplayed();

        var owner = sourceCard.Owner;
        if (owner == null) 
        {
            MainFile.Logger?.Warn($"[ReplayHelper] Owner is null, cannot execute replay");
            return;
        }
        
        var playerCardPool = owner.Character?.CardPool;
        MainFile.Logger?.Info($"[ReplayHelper] Executing replay for owner: {owner.GetType().Name}, cardPool: {playerCardPool?.Id?.Entry ?? "null"}");
        
        // 获取本局游戏中从牌组移除的卡牌
        var removedCards = RemovedCardsTracker.RemovedCards;

        if (removedCards.Count == 0)
        {
            MainFile.Logger?.Info($"[ReplayHelper] No cards have been removed from deck this run");
            return;
        }

        // 过滤掉本场战斗中已经被重现过的卡牌
        var availableCards = removedCards
            .Where(c => !RemovedCardsTracker.HasBeenReplayedThisCombat(c))
            .ToList();

        if (availableCards.Count == 0)
        {
            MainFile.Logger?.Info($"[ReplayHelper] All removed cards have been replayed this combat");
            return;
        }

        // 将 SerializableCard 转换为 CardModel 用于显示
        // 只保留属于当前玩家角色的卡牌，避免其他角色的卡牌混入
        var displayCards = availableCards
            .Select(c => {
                var card = CardModel.FromSerializable(c);
                if (card != null)
                {
                    card.Owner = owner; // 设置拥有者，否则 CardSelectCmd 会报错
                }
                return card;
            })
            .Where(c => c != null && (playerCardPool == null || c.Pool?.Id == playerCardPool.Id))
            .Cast<CardModel>()
            .ToList();

        if (displayCards.Count == 0)
        {
            MainFile.Logger?.Warn($"[ReplayHelper] Failed to deserialize any removed cards");
            return;
        }

        // 让玩家选择卡牌
        var maxSelect = Math.Min(count, displayCards.Count);
        
        var selectionPrompt = new LocString("static_hover_tips", "choose_cards_to_replay");

        var prefs = new CardSelectorPrefs(
            selectionPrompt,
            maxSelect,
            maxSelect
        )
        {
            Cancelable = false
        };

        var selectedCards = (await CardSelectCmd.FromSimpleGrid(
            context,
            displayCards,
            owner,
            prefs
        )).ToList();
        
        if (selectedCards.Count == 0)
        {
            MainFile.Logger?.Warn($"[ReplayHelper] No cards were selected");
            return;
        }

        // 处理每张选中的卡牌
        foreach (var selectedCard in selectedCards)
        {
            // 找到对应的 SerializableCard 并标记为已重现
            var serializableCard = availableCards.FirstOrDefault(c => 
                c.Id?.Entry == selectedCard.Id.Entry && 
                c.CurrentUpgradeLevel == selectedCard.CurrentUpgradeLevel);
            
            if (serializableCard != null)
            {
                RemovedCardsTracker.MarkReplayed(serializableCard);
            }

            // 复制卡牌：使用 CombatState.CreateCard 确保卡牌在战斗状态中
            CardModel copiedCard;
            if (serializableCard != null && serializableCard.Id != null)
            {
                var canonicalCard = ModelDb.GetById<CardModel>(serializableCard.Id);
                if (canonicalCard != null)
                {
                    copiedCard = combatState.CreateCard(canonicalCard, owner);
                    // 应用升级等级
                    while (copiedCard.CurrentUpgradeLevel < serializableCard.CurrentUpgradeLevel)
                    {
                        copiedCard.UpgradeInternal();
                    }
                    // 应用附魔
                    if (serializableCard.Enchantment != null)
                    {
                        var enchantment = EnchantmentModel.FromSerializable(serializableCard.Enchantment);
                        if (enchantment != null)
                        {
                            CardCmd.Enchant(enchantment, copiedCard, serializableCard.Enchantment.Amount);
                        }
                    }
                }
                else
                {
                    copiedCard = selectedCard.CreateClone();
                    copiedCard.Owner = owner;
                    if (!combatState.ContainsCard(copiedCard))
                    {
                        combatState.AddCard(copiedCard, owner);
                    }
                }
            }
            else
            {
                copiedCard = selectedCard.CreateClone();
                copiedCard.Owner = owner;
                if (!combatState.ContainsCard(copiedCard))
                {
                    combatState.AddCard(copiedCard, owner);
                }
            }

            if (copiedCard == null)
            {
                MainFile.Logger?.Warn($"[ReplayHelper] Failed to create copy of card");
                continue;
            }

            // 添加消耗属性
            copiedCard.AddKeyword(CardKeyword.Exhaust);

            // 本回合耗能-1
            copiedCard.EnergyCost.AddThisTurnOrUntilPlayed(-1);

            // 如果需要，在整局游戏中升级
            if (upgradeForRun && !copiedCard.IsUpgraded)
            {
                copiedCard.UpgradeInternal();
                copiedCard.FinalizeUpgradeInternal();
                MainFile.Logger?.Info($"[ReplayHelper] Upgraded card for run: {copiedCard.GetType().Name}");
            }

            // 加入手牌
            await CardPileCmd.Add(copiedCard, PileType.Hand);

            MainFile.Logger?.Info($"[ReplayHelper] Replayed card: {selectedCard.GetType().Name}");
        }
    }
}
