using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Theresa.TheresaCode.Utils;

/// <summary>
/// 追踪本局游戏中从牌组永久移除的卡牌
/// 用于"重现"机制：检视移除过的卡牌，选择复制到手牌
/// </summary>
public static class RemovedCardsTracker
{
    /// <summary>
    /// 本局游戏中所有从牌组移除的卡牌（序列化形式，用于跨场景保存）
    /// </summary>
    private static readonly List<SerializableCard> _removedCards = new();

    /// <summary>
    /// 本场战斗中已经被重现过的卡牌ID集合
    /// </summary>
    private static readonly HashSet<string> _replayedThisCombat = new();

    /// <summary>
    /// 获取本局游戏中所有从牌组移除的卡牌
    /// </summary>
    public static IReadOnlyList<SerializableCard> RemovedCards => _removedCards.AsReadOnly();

    /// <summary>
    /// 添加一张被移除的卡牌到追踪列表
    /// </summary>
    public static void AddRemovedCard(SerializableCard card)
    {
        if (card?.Id == null) return;
        
        // 避免重复添加相同的卡牌
        var key = GetCardKey(card);
        if (!_removedCards.Any(c => GetCardKey(c) == key))
        {
            _removedCards.Add(card);
            MainFile.Logger?.Info($"[RemovedCardsTracker] Tracked removed card: {card.Id.Entry}");
        }
    }

    /// <summary>
    /// 检查卡牌是否在本场战斗中已经被重现过
    /// </summary>
    public static bool HasBeenReplayedThisCombat(SerializableCard card)
    {
        return _replayedThisCombat.Contains(GetCardKey(card));
    }

    /// <summary>
    /// 标记卡牌在本场战斗中已被重现
    /// </summary>
    public static void MarkReplayed(SerializableCard card)
    {
        _replayedThisCombat.Add(GetCardKey(card));
    }

    /// <summary>
    /// 战斗开始时重置本场战斗的重现记录
    /// </summary>
    public static void OnCombatStart()
    {
        _replayedThisCombat.Clear();
        MainFile.Logger?.Info($"[RemovedCardsTracker] Combat started, replay tracking reset. Total removed cards: {_removedCards.Count}");
    }

    /// <summary>
    /// 新游戏开始时重置所有记录
    /// </summary>
    public static void OnNewRun()
    {
        _removedCards.Clear();
        _replayedThisCombat.Clear();
        MainFile.Logger?.Info("[RemovedCardsTracker] New run started, all tracking reset");
    }

    private static string GetCardKey(SerializableCard card)
    {
        return $"{card.Id?.Entry}_{card.CurrentUpgradeLevel}";
    }
}

/// <summary>
/// Harmony Patch: 拦截 CardPileCmd.RemoveFromDeck 来追踪被移除的卡牌
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.RemoveFromDeck))]
[HarmonyPatch([typeof(IReadOnlyList<CardModel>), typeof(bool)])]
public static class RemoveFromDeckPatch
{
    [HarmonyPrefix]
    public static void Prefix(IReadOnlyList<CardModel> cards)
    {
        if (cards == null) return;

        foreach (var card in cards)
        {
            if (card == null) continue;
            
            // 记录被移除的卡牌
            RemovedCardsTracker.AddRemovedCard(card.ToSerializable());
        }
    }
}
