using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using Theresa.TheresaCode.Relics;

namespace Theresa.TheresaCode.Utils;

/// <summary>
/// 追踪本局游戏中从牌组永久移除的卡牌
/// 用于"重现"机制：检视移除过的卡牌，选择复制到手牌
/// 
/// 注意：本类维护的静态列表仅在当前进程内存中有效。
/// 联机模式下 Client rejoin/读档时不会自动同步，真正的持久化/同步由 UnknownRelic/KnownRelic 的 RemovedCards（SavedProperty）负责。
/// 因此 ReplayHelper 执行前会先调用 <see cref="SyncFromRelic(Player)"/> 把遗物上的数据拉回到本静态缓存。
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
    /// <param name="card">被移除的卡牌</param>
    /// <param name="owner">卡牌拥有者（用于同步到遗物，保证联机一致）</param>
    public static void AddRemovedCard(SerializableCard card, Player? owner = null)
    {
        if (card?.Id == null) return;

        // 避免重复添加相同的卡牌
        var key = GetCardKey(card);
        if (!_removedCards.Any(c => GetCardKey(c) == key))
        {
            _removedCards.Add(card);
            MainFile.Logger?.Info($"[RemovedCardsTracker] Tracked removed card: {card.Id.Entry}");
        }

        // 同步到 UnknownRelic/KnownRelic，确保联机时随 RunState 一起保存/同步
        GetRemovedCardsHolder(owner)?.TrackRemovedCard(card);
    }

    /// <summary>
    /// 从 Theresa 遗物同步已移除卡牌列表到本静态缓存。
    /// 在联机读档/rejoin 后，遗物数据已从 Host 同步过来，但静态列表为空，需要显式同步。
    /// </summary>
    public static void SyncFromRelic(Player owner)
    {
        var relic = GetRemovedCardsHolder(owner);
        if (relic == null) return;

        var tracked = relic.GetTrackedRemovedCards();
        int added = 0;
        foreach (var card in tracked)
        {
            if (card?.Id == null) continue;
            var key = GetCardKey(card);
            if (!_removedCards.Any(c => GetCardKey(c) == key))
            {
                _removedCards.Add(card);
                added++;
            }
        }

        MainFile.Logger?.Info($"[RemovedCardsTracker] Synced {added} removed cards from relic (total {_removedCards.Count})");
    }

    /// <summary>
    /// 获取持有已移除卡牌记录的 Theresa 遗物（UnknownRelic 或 KnownRelic）。
    /// </summary>
    private static TheresaRelicModel? GetRemovedCardsHolder(Player? owner)
    {
        if (owner == null) return null;

        // 优先查找 KnownRelic（UnknownRelic 升级后的形态）
        var known = owner.Relics.OfType<KnownRelic>().FirstOrDefault();
        if (known != null) return known;

        // 否则查找 UnknownRelic
        var unknown = owner.Relics.OfType<UnknownRelic>().FirstOrDefault();
        if (unknown != null) return unknown;

        return null;
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

    /// <summary>
    /// 从存档恢复被移除的卡牌列表（单机读档时调用，作为遗物同步之外的兜底）
    /// </summary>
    public static void RestoreFromSave(List<SerializableCard>? cards)
    {
        _removedCards.Clear();
        if (cards != null)
        {
            foreach (var card in cards)
            {
                if (card?.Id == null) continue;
                var key = GetCardKey(card);
                if (!_removedCards.Any(c => GetCardKey(c) == key))
                {
                    _removedCards.Add(card);
                }
            }
            MainFile.Logger?.Info($"[RemovedCardsTracker] Restored {_removedCards.Count} removed cards from save");
        }
        else
        {
            MainFile.Logger?.Info("[RemovedCardsTracker] No removed cards data in save, tracker cleared");
        }
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

            // 记录被移除的卡牌，同时传入 Owner 以同步到遗物
            RemovedCardsTracker.AddRemovedCard(card.ToSerializable(), card.Owner);
        }
    }
}

/// <summary>
/// Harmony Patch: 新一局游戏开始时重置 RemovedCardsTracker 与 Theresa 遗物的运行时数据。
/// 防止 ModelDb canonical 实例复用导致上一局的已移除卡牌跨存档串扰。
/// </summary>
[HarmonyPatch(typeof(RunManager), "InitializeNewRun")]
public static class RunManagerInitializeNewRunPatch
{
    [HarmonyPostfix]
    public static void Postfix(RunManager __instance)
    {
        RemovedCardsTracker.OnNewRun();

        // 重置 canonical 实例，避免本局后续通过 ToMutable() 新获得的遗物携带旧数据
        (ModelDb.Relic<UnknownRelic>() as TheresaRelicModel)?.ResetForNewRun();
        (ModelDb.Relic<KnownRelic>() as TheresaRelicModel)?.ResetForNewRun();

        // 重置起始遗物上已经产生的 mutable 副本
        var state = __instance.DebugOnlyGetState();
        if (state?.Players == null) return;
        foreach (var player in state.Players)
        {
            foreach (var relic in player.Relics.OfType<TheresaRelicModel>())
            {
                relic.ResetForNewRun();
            }
        }

        MainFile.Logger?.Info("[RunManagerInitializeNewRunPatch] RemovedCardsTracker and Theresa relics reset for new run.");
    }
}

/// <summary>
/// Harmony Patch: 从存档继续/加入联机房间时重置 canonical 实例与静态缓存。
/// 玩家的 mutable 遗物会在 FromSerializable 后由 SavedProperty 恢复，因此这里只清 canonical 与 tracker。
/// </summary>
[HarmonyPatch(typeof(RunManager), "InitializeSavedRun")]
public static class RunManagerInitializeSavedRunPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        RemovedCardsTracker.OnNewRun();

        (ModelDb.Relic<UnknownRelic>() as TheresaRelicModel)?.ResetForNewRun();
        (ModelDb.Relic<KnownRelic>() as TheresaRelicModel)?.ResetForNewRun();

        MainFile.Logger?.Info("[RunManagerInitializeSavedRunPatch] RemovedCardsTracker and canonical Theresa relics reset after loading save.");
    }
}
