using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using MegaCrit.Sts2.Core.TestSupport;
using Theresa.TheresaCode.Actions;
using Theresa.TheresaCode.Dust.Nodes;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Powers;
using Theresa.TheresaCode.Relics;

namespace Theresa.TheresaCode.Dust;

/// <summary>
/// 微尘管理器 - 管理环绕角色的微尘卡牌。
/// 
/// 网络同步原则：
/// 1. 每个玩家的微尘状态按 <see cref="Player"/> 隔离，避免多玩家间串扰。
/// 2. 改变微尘列表的操作（添加/移除/打出）应通过对应的 <see cref="GameAction"/> 执行，
///    使 host/client 保持一致的操作序列与最终状态。
/// 3. <see cref="DustIt"/> 通过 <see cref="DustItAction"/> 同步，由触发端随机选牌后
///    将 <see cref="NetCombatCard"/> 广播给所有客户端，确保两端打出同一张牌。
/// </summary>
public static class DustManager
{
    public const int BaseMaxDust = 3;
    public const int MaxDustLimit = 10;

    /// <summary>
    /// 单个玩家的微尘运行时状态。
    /// </summary>
    internal sealed class DustState
    {
        public readonly List<CardModel> Cards = [];
        public int MaxDustModifier = 0;
        public readonly HashSet<CardModel> CurrentlyLingering = [];
    }

    private static readonly SpireField<Player, DustState> PlayerStates = new(_ => new DustState());

    private static DustState GetState(Player player)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        return PlayerStates[player]!;
    }

    private static Player? ResolvePlayer(Player? player)
    {
        if (player != null) return player;
        // RunManager.State 是 private，无法直接访问；战斗中可通过 CombatManager 拿到 ICombatState。
        return LocalContext.GetMe(CombatManager.Instance.DebugOnlyGetState());
    }

    #region Queries

    public static int MaxDust(Player? player = null)
    {
        var p = ResolvePlayer(player);
        if (p == null) return BaseMaxDust;
        return BaseMaxDust + GetState(p).MaxDustModifier;
    }

    public static bool IsFull(Player? player = null)
    {
        var p = ResolvePlayer(player);
        if (p == null) return false;
        var state = GetState(p);
        return state.Cards.Count >= BaseMaxDust + state.MaxDustModifier;
    }

    /// <summary>
    /// 返回所有玩家的所有微尘卡牌（向后兼容，UI/提示等场景仍可使用）。
    /// </summary>
    public static IReadOnlyList<CardModel> Cards
    {
        get
        {
            var result = new List<CardModel>();
            // RunManager.State 是 private，通过 CombatManager 在战斗中获取玩家列表。
            var players = CombatManager.Instance.DebugOnlyGetState()?.Players;
            if (players != null)
            {
                foreach (var player in players)
                {
                    result.AddRange(GetState(player).Cards);
                }
            }
            return result.AsReadOnly();
        }
    }

    public static IReadOnlyList<CardModel> CardsFor(Player player)
    {
        return GetState(player).Cards.AsReadOnly();
    }

    public static bool IsCurrentlyLingering(CardModel card)
    {
        if (card?.Owner == null) return false;
        return GetState(card.Owner).CurrentlyLingering.Contains(card);
    }

    #endregion

    #region Max Dust Modifier

    /// <summary>
    /// 增加微尘上限（用于魔王传承等效果）。此操作直接在同步上下文中执行，
    /// 调用方应确保处于 GameAction/Power/Relic 的同步回调中。
    /// </summary>
    public static void IncreaseMaxDust(int amount, Player? player = null)
    {
        var p = ResolvePlayer(player);
        if (p == null) return;
        ModifyMaxDust(amount, p);
    }

    public static void ModifyMaxDust(int delta, Player? player = null)
    {
        var p = ResolvePlayer(player);
        if (p == null) return;
        var state = GetState(p);
        state.MaxDustModifier += delta;
        if (BaseMaxDust + state.MaxDustModifier > MaxDustLimit)
            state.MaxDustModifier = MaxDustLimit - BaseMaxDust;
        else if (BaseMaxDust + state.MaxDustModifier < 0)
            state.MaxDustModifier = -BaseMaxDust;
    }

    public static void ResetMaxDust(Player? player = null)
    {
        var p = ResolvePlayer(player);
        if (p == null) return;
        GetState(p).MaxDustModifier = 0;
    }

    #endregion

    #region Lifecycle

    public static void PreBattle(Player player)
    {
        var state = GetState(player);
        MainFile.Logger?.Info($"[DustManager] PreBattle for player {player.NetId}: clearing {state.Cards.Count} cards, resetting max dust modifier");
        state.Cards.Clear();
        state.CurrentlyLingering.Clear();
        state.MaxDustModifier = 0;
        UpdateVisualsFor(player);
    }

    public static void PostBattle(Player player)
    {
        var state = GetState(player);
        MainFile.Logger?.Info($"[DustManager] PostBattle for player {player.NetId}: clearing {state.Cards.Count} cards");
        state.Cards.Clear();
        state.CurrentlyLingering.Clear();
        UpdateVisualsFor(player);
    }

    public static void AtTurnEnd(Player? player = null)
    {
        IEnumerable<Player> players;
        if (player != null)
            players = [player];
        else
            // RunManager.State 是 private，通过 CombatManager 在战斗中获取玩家列表。
            players = CombatManager.Instance.DebugOnlyGetState()?.Players ?? Enumerable.Empty<Player>();

        foreach (var p in players)
        {
            var state = GetState(p);
            foreach (var card in state.Cards.ToList())
            {
                if (card is IDustCard dustCard)
                {
                    dustCard.AtTurnEndIfDust();
                }
            }
        }
    }

    #endregion

    #region Add / Remove

    /// <summary>
    /// 检查同一张卡牌实例是否已经在微尘中。
    /// 使用实例引用而非 ID.Entry 判断，允许同名卡同时存在于微尘。
    /// </summary>
    public static bool ContainsCard(CardModel card)
    {
        if (card?.Owner == null) return false;
        return GetState(card.Owner).Cards.Any(c => c == card);
    }

    public static async Task AddCard(CardModel card)
    {
        await AddCardAtIndex(card, -1);
    }

    /// <summary>
    /// 添加卡牌到微尘，可超出当前 MaxDust 限制（但不超过 MaxDustLimit）。
    /// 用于 SadDust 等卡牌效果。
    /// </summary>
    public static async Task AddCardOverLimit(CardModel card)
    {
        if (card?.Owner == null) return;

        // Dim 卡牌不应进入微尘；这是最后一道防线，防止绕过 ShouldBecomeDust 的调用。
        if (card.Keywords.Contains(DimKeyword.Dim))
        {
            MainFile.Logger?.Info($"[DustManager] AddCardOverLimit: rejected Dim card {card.Id.Entry} from dust");
            return;
        }

        var player = card.Owner;
        var state = GetState(player);

        if (ContainsCard(card))
        {
            MainFile.Logger?.Info($"[DustManager] AddCardOverLimit: card {card.Id.Entry} already in dust, skipping. Current dust: {string.Join(", ", state.Cards.Select(c => c.Id.Entry))}");
            return;
        }

        // 如果已达绝对上限，将最旧的微尘牌（末尾）移回弃牌堆
        if (state.Cards.Count >= MaxDustLimit)
        {
            var oldestCard = state.Cards.LastOrDefault();
            if (oldestCard != null)
            {
                MainFile.Logger?.Info($"[DustManager] AddCardOverLimit: reached limit {MaxDustLimit}, removing oldest {oldestCard.Id.Entry}");
                await RemoveCard(oldestCard);
                await CardPileCmd.Add(oldestCard, PileType.Discard);
            }
        }

        // 微尘不是标准牌堆，确保卡牌已从原牌堆移除。
        // Pile 是计算属性，只要从原牌堆列表移除即可。
        if (card.Pile != null)
        {
            card.RemoveFromCurrentPile();
        }

        state.Cards.Add(card);
        MainFile.Logger?.Info($"[DustManager] AddCardOverLimit: added {card.Id.Entry}. Current dust: {string.Join(", ", state.Cards.Select(c => c.Id.Entry))}");

        // 播放卡牌飞入微尘动画（本地玩家）
        if (LocalContext.IsMe(card.Owner) && !TestMode.IsOn)
        {
            await PlayCardToDustAnimation(card);
        }

        // 触发 PastDustPower
        if (card.Owner?.Creature != null)
        {
            var pastDust = card.Owner.Creature.GetPower<PastDustPower>();
            if (pastDust != null)
            {
                await pastDust.TriggerOnBecomeDust(card);
            }
        }

        // 触发卡牌回调
        if (card is IDustCard dustCard)
        {
            dustCard.TriggerWhenBecomeDust();
        }

        // 超出上限时不增加 MantraPower（SadDust 等效果不应增加）
        UpdateVisualsFor(player);
    }

    public static async Task AddCardAtIndex(CardModel card, int index)
    {
        if (card?.Owner == null) return;

        // Dim 卡牌不应进入微尘；这是最后一道防线，防止绕过 ShouldBecomeDust 的调用。
        if (card.Keywords.Contains(DimKeyword.Dim))
        {
            MainFile.Logger?.Info($"[DustManager] AddCardAtIndex: rejected Dim card {card.Id.Entry} from dust");
            return;
        }

        var player = card.Owner;
        var state = GetState(player);

        if (ContainsCard(card))
        {
            MainFile.Logger?.Info($"[DustManager] AddCardAtIndex: card {card.Id.Entry} already in dust, skipping. Current dust: {string.Join(", ", state.Cards.Select(c => c.Id.Entry))}");
            return;
        }

        // 如果已达当前最大微尘数量限制，将最旧的微尘牌（末尾）移回弃牌堆
        if (state.Cards.Count >= BaseMaxDust + state.MaxDustModifier)
        {
            var oldestCard = state.Cards.LastOrDefault();
            if (oldestCard != null)
            {
                MainFile.Logger?.Info($"[DustManager] AddCardAtIndex: reached max {BaseMaxDust + state.MaxDustModifier}, removing oldest {oldestCard.Id.Entry}");
                await RemoveCard(oldestCard);
                await CardPileCmd.Add(oldestCard, PileType.Discard);
            }
        }

        // 微尘不是标准牌堆，确保卡牌已从原牌堆移除。
        // Pile 是计算属性，只要从原牌堆列表移除即可。
        if (card.Pile != null)
        {
            card.RemoveFromCurrentPile();
        }

        if (index >= 0 && index < state.Cards.Count)
            state.Cards.Insert(index, card);
        else
            state.Cards.Add(card);

        MainFile.Logger?.Info($"[DustManager] AddCardAtIndex: added {card.Id.Entry} at index {index}. Current dust: {string.Join(", ", state.Cards.Select(c => c.Id.Entry))}");

        // 播放卡牌飞入微尘动画（本地玩家）
        if (LocalContext.IsMe(card.Owner) && !TestMode.IsOn)
        {
            await PlayCardToDustAnimation(card);
        }

        // 触发 PastDustPower
        if (card.Owner?.Creature != null)
        {
            var pastDust = card.Owner.Creature.GetPower<PastDustPower>();
            if (pastDust != null)
            {
                await pastDust.TriggerOnBecomeDust(card);
            }
        }

        // 触发卡牌回调
        if (card is IDustCard dustCard)
        {
            dustCard.TriggerWhenBecomeDust();
        }

        // 同步增加等量 MantraPower（仅当 Dust 数量高于 MaxDust 时，即溢出时）
        if (card.Owner?.Creature != null && state.Cards.Count > BaseMaxDust + state.MaxDustModifier)
        {
            await PowerCmd.Apply<MantraPower>(new ThrowingPlayerChoiceContext(), card.Owner.Creature, 1, card.Owner.Creature, null);
        }

        UpdateVisualsFor(player);
    }

    public static async Task RemoveCard(CardModel card)
    {
        if (card?.Owner == null) return;
        var player = card.Owner;
        var state = GetState(player);
        MainFile.Logger?.Info($"[DustManager] RemoveCard: removing {card.Id.Entry}. Current dust before: {string.Join(", ", state.Cards.Select(c => c.Id.Entry))}");
        RemoveCardInternal(card);
        MainFile.Logger?.Info($"[DustManager] RemoveCard: removed {card.Id.Entry}. Current dust after: {string.Join(", ", state.Cards.Select(c => c.Id.Entry))}");

        // 同步减少等量 MantraPower
        if (card.Owner?.Creature != null)
        {
            var mantra = card.Owner.Creature.GetPower<MantraPower>();
            if (mantra != null)
            {
                await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), mantra, -1, card.Owner.Creature, null);
            }
        }
    }

    internal static void RemoveCardInternal(CardModel card)
    {
        if (card?.Owner == null) return;
        var state = GetState(card.Owner);
        var toRemove = state.Cards.FirstOrDefault(c => c == card || (c.Id.Entry == card.Id.Entry && c.Owner == card.Owner));
        if (toRemove == null) return;
        state.Cards.Remove(toRemove);

        if (card is IDustCard dustCard)
        {
            dustCard.TriggerWhenNoLongerDust();
        }

        UpdateVisualsFor(card.Owner);
    }

    /// <summary>
    /// 强制移除一张 Dust 卡（不减少 Mantra）。用于 Mantra &lt; 3 时的矫正机制。
    /// </summary>
    public static void ForceRemoveDust(CardModel card)
    {
        RemoveCardInternal(card);
    }

    #endregion

    #region Block Damage

    /// <summary>
    /// 抵挡伤害。
    /// </summary>
    public static int BlockDamage(int damageAmount, Creature player)
    {
        if (damageAmount <= 0 || player == null) return damageAmount;
        var ownerPlayer = player.Player;
        if (ownerPlayer == null) return damageAmount;
        var state = GetState(ownerPlayer);

        var cardsToExhaust = new List<CardModel>();
        int originalDamage = damageAmount;

        foreach (var card in state.Cards.ToList())
        {
            int blockAmt = 0;
            if (card is IDustCard dustCard)
            {
                blockAmt = dustCard.BlockDamageIfDust();
                if (blockAmt > 0 && dustCard.ExhaustAfterBlockDamage)
                    cardsToExhaust.Add(card);
            }

            damageAmount -= blockAmt;
            if (damageAmount <= 0)
            {
                damageAmount = 0;
                break;
            }
        }

        if (cardsToExhaust.Count > 0)
        {
            MainFile.Logger?.Info($"[DustManager] BlockDamage: blocked {originalDamage - damageAmount} of {originalDamage}, exhausting cards: {string.Join(", ", cardsToExhaust.Select(c => c.Id.Entry))}");
        }

        foreach (var card in cardsToExhaust)
        {
            RemoveCardInternal(card);
            _ = CardPileCmd.Add(card, PileType.Exhaust);
            _ = PowerCmd.Apply<MantraPower>(new ThrowingPlayerChoiceContext(), player, -1, player, null);
        }

        return damageAmount;
    }

    #endregion

    #region DustIt (Linger)

    /// <summary>
    /// 萦绕：随机打出一张微尘牌（旧版：通过 <see cref="DustItAction"/> 入队执行）。
    /// 当前所有调用方已改为 <see cref="DustItSync"/>，保留此方法以防外部调用。
    /// </summary>
    public static Task DustIt(Player player, bool toTop, bool exhaustIt)
    {
        if (player == null) return Task.CompletedTask;
        var state = GetState(player);
        if (state.Cards.Count == 0)
        {
            MainFile.Logger?.Info("[DustManager] DustIt: no dust cards, skipping");
            return Task.CompletedTask;
        }

        var synchronizer = RunManager.Instance?.ActionQueueSynchronizer;
        if (synchronizer == null)
        {
            // 非战斗/无网络环境，直接本地执行（主要用于测试）
            return DustItLocalAsync(player, toTop, exhaustIt);
        }

        synchronizer.RequestEnqueue(new DustItAction(player, toTop, exhaustIt));
        return Task.CompletedTask;
    }

    /// <summary>
    /// 基础萦绕入口（每回合1次），由遗物/回合开始时调用。
    /// 直接在同步路径中执行，避免在回合开始 checksum 生成前 enqueue 异步 GameAction 导致状态分歧。
    /// </summary>
    public static async Task AtTurnStartPostDraw(Player player)
    {
        if (player?.Creature?.CombatState == null) return;
        await DustItSync(player, false, false);
    }

    /// <summary>
    /// 同步执行一次萦绕：用于已经处于同步上下文中的调用方（如 SetupPlayerTurn、GameAction 执行中）。
    /// 使用同步 RNG 在本地直接选牌并打出，所有客户端会得出相同结果。
    /// </summary>
    public static Task DustItSync(Player player, bool toTop, bool exhaustIt)
    {
        if (player == null) return Task.CompletedTask;
        var state = GetState(player);
        if (state.Cards.Count == 0)
        {
            MainFile.Logger?.Info("[DustManager] DustItSync: no dust cards, skipping");
            return Task.CompletedTask;
        }
        return DustItLocalAsync(player, toTop, exhaustIt);
    }

    /// <summary>
    /// 本地执行一次萦绕（用于单机或无 ActionQueueSynchronizer 环境，以及已经处于同步路径中的调用方）。
    /// </summary>
    private static async Task DustItLocalAsync(Player player, bool toTop, bool exhaustIt)
    {
        var state = GetState(player);
        var playableCards = state.Cards
            .Where(c => c.Owner == player && !state.CurrentlyLingering.Contains(c))
            .ToList();
        if (playableCards.Count == 0) return;

        var rng = player.RunState.Rng.Shuffle;
        for (int i = playableCards.Count - 1; i > 0; i--)
        {
            int j = rng.NextInt(i + 1);
            (playableCards[i], playableCards[j]) = (playableCards[j], playableCards[i]);
        }

        var lingeredCard = playableCards[0];
        Creature? target = null;
        var combatState = player.Creature?.CombatState;
        if (combatState != null)
        {
            if (lingeredCard.TargetType == TargetType.AnyEnemy)
                target = player.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies);
            else if (lingeredCard.TargetType == TargetType.AnyAlly)
                target = player.RunState.Rng.CombatTargets.NextItem(combatState.Allies.Where(c => c != null && c.IsAlive));
            else if (lingeredCard.TargetType == TargetType.Self)
                target = player.Creature;
        }

        await ExecuteLingeredCard(player, lingeredCard, toTop, exhaustIt, target?.CombatId, new ThrowingPlayerChoiceContext());
    }

    /// <summary>
    /// 执行一张已被选定的微尘牌的萦绕效果（动画 + 打出 + 处理 exhaust）。
    /// 由 <see cref="DustItAction.ExecuteAction"/> 调用，保证两端执行一致。
    /// </summary>
    internal static async Task ExecuteLingeredCard(Player player, CardModel lingeredCard, bool toTop, bool exhaustIt, uint? targetCombatId, PlayerChoiceContext choiceContext)
    {
        var state = GetState(player);

        // 标记该卡正在萦绕，防止递归调用 DustIt 时再次选中它
        state.CurrentlyLingering.Add(lingeredCard);
        try
        {
            MainFile.Logger?.Info($"[DustManager] ExecuteLingeredCard: selected {lingeredCard.Id.Entry} from dust [{string.Join(", ", state.Cards.Select(c => c.Id.Entry))}]");
            bool hasExhaust = lingeredCard.Keywords.Contains(CardKeyword.Exhaust);
            bool handledByCard = false;

            if (lingeredCard is IDustCard dustCard)
            {
                handledByCard = await dustCard.TriggerWhenLingered();
                if (dustCard.ShouldExhaust())
                    hasExhaust = true;
                if (dustCard.DontExhaustIfExhaust)
                    hasExhaust = false;
            }

            // 通知 BabelWord 遗物记录被萦绕的卡牌
            NotifyBabelWord(player, lingeredCard);

            // 如果卡自己处理了萦绕（如移到手牌），跳过后续打出逻辑
            if (handledByCard)
            {
                if (hasExhaust && ContainsCard(lingeredCard))
                {
                    await RemoveCard(lingeredCard);
                    await CardPileCmd.Add(lingeredCard, PileType.Exhaust);
                }
                return;
            }

            if (player.Creature == null) return;
            var combatState = player.Creature.CombatState;
            if (combatState == null) return;

            // 创建副本
            var copy = lingeredCard.CreateClone();
            // 副本通过 MemberwiseClone 可能已继承原卡 Owner；仅在缺失时补全。
            if (copy.Owner == null)
            {
                copy.Owner = player;
            }

            // 安全过滤：确保 AutoPlay 不会访问空 Owner 或已死亡/结束战斗的 Owner
            if (copy.Owner == null || copy.Owner.Creature is not { IsDead: false } || CombatManager.Instance.IsOverOrEnding)
            {
                MainFile.Logger?.Info($"[DustManager] ExecuteLingeredCard: clone owner invalid or combat ending for {copy.Id.Entry}, skipping play");
                return;
            }

            // 确定目标
            Creature? target = null;
            if (targetCombatId.HasValue)
            {
                target = await combatState.GetCreatureAsync(targetCombatId.Value, 10.0);
            }

            if (target == null && copy.TargetType == TargetType.Self)
            {
                target = player.Creature;
            }

            // 本地玩家：自定义 Dust 飞出动画
            var combatUi = NCombatRoom.Instance?.Ui;
            if (LocalContext.IsMe(player) && !TestMode.IsOn && combatUi != null)
            {
                var visualCard = NCard.Create(copy);
                if (visualCard != null)
                {
                    try
                    {
                        var playerNode = NCombatRoom.Instance!.GetCreatureNode(player.Creature);
                        Vector2 startGlobalPos = NDustRing.Instance?.GetCardGlobalPosition(lingeredCard)
                            ?? playerNode?.GlobalPosition
                            ?? Vector2.Zero;

                        combatUi.AddToPlayContainer(visualCard);
                        visualCard.GlobalPosition = startGlobalPos;
                        visualCard.Scale = Vector2.One * 0.5f;
                        visualCard.UpdateVisuals(PileType.Play, CardPreviewMode.Normal);

                        Vector2 targetPos = PileType.Play.GetTargetPosition(visualCard);
                        var flyTween = visualCard.CreateTween();
                        if (flyTween != null)
                        {
                            flyTween.SetParallel(true);
                            flyTween.TweenProperty(visualCard, "position", targetPos, 0.4f)
                                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
                            flyTween.TweenProperty(visualCard, "scale", Vector2.One * 1.1f, 0.4f)
                                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);

                            await flyTween.AwaitFinished(visualCard);
                            flyTween.Kill();
                        }

                        visualCard.GetParent()?.RemoveChildSafely(visualCard);
                        visualCard.Visible = false;

                        await CardCmd.AutoPlay(choiceContext, copy, target, AutoPlayType.Default, skipXCapture: false, skipCardPileVisuals: true);

                        visualCard.Visible = true;
                        combatUi.AddToPlayContainer(visualCard);

                        var exhaustVfx = NExhaustVfx.Create(visualCard);
                        if (exhaustVfx != null)
                        {
                            combatUi.AddChildSafely(exhaustVfx);
                        }

                        var fadeTween = visualCard.CreateTween();
                        if (fadeTween != null)
                        {
                            fadeTween.SetParallel(true);
                            fadeTween.TweenProperty(visualCard, "modulate:a", 0f, 0.3f);
                            fadeTween.TweenProperty(visualCard, "scale", Vector2.Zero, 0.3f);

                            await fadeTween.AwaitFinished(visualCard);
                            fadeTween.Kill();
                        }

                        if (GodotObject.IsInstanceValid(visualCard))
                        {
                            visualCard.Modulate = Colors.White;
                            visualCard.Scale = Vector2.One;
                            visualCard.QueueFreeSafely();
                        }
                    }
                    catch (Exception ex)
                    {
                        MainFile.Logger?.Info($"[DustManager] ExecuteLingeredCard animation failed for {copy.Id.Entry}: {ex.Message}");
                        if (GodotObject.IsInstanceValid(visualCard))
                        {
                            visualCard.GetParent()?.RemoveChildSafely(visualCard);
                            visualCard.QueueFreeSafely();
                        }
                        await CardCmd.AutoPlay(choiceContext, copy, target);
                    }
                }
                else
                {
                    await CardCmd.AutoPlay(choiceContext, copy, target);
                }
            }
            else
            {
                await CardCmd.AutoPlay(choiceContext, copy, target);
            }

            // 复制牌直接从战斗中移除，不进入任何牌堆（像 Cure 一样）。
            try
            {
                await CardPileCmd.RemoveFromCombat(copy);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("combat pile"))
            {
                MainFile.Logger?.Info($"[DustManager] ExecuteLingeredCard: copy {copy.Id.Entry} already removed from combat piles, skipping RemoveFromCombat");
            }

            // 萦绕打出的牌不从 Dust 中移除，但带 Exhaust 的原牌会被消耗
            if (hasExhaust)
            {
                await RemoveCard(lingeredCard);
                await CardPileCmd.Add(lingeredCard, PileType.Exhaust);
            }
        }
        finally
        {
            state.CurrentlyLingering.Remove(lingeredCard);
        }
    }

    /// <summary>
    /// 通知 BabelWord 遗物记录被萦绕的卡牌。
    /// </summary>
    private static void NotifyBabelWord(Player? player, CardModel card)
    {
        if (player?.Creature == null) return;

        var babelWord = player.GetRelic<BabelWord>();
        babelWord?.OnCardLingered(card);
    }

    #endregion

    #region ShouldBecomeDust

    /// <summary>
    /// 检查卡牌是否应该转化为微尘。
    /// </summary>
    public static bool ShouldBecomeDust(CardModel card)
    {
        if (card?.Owner == null) return false;
        if (IsFull(card.Owner)) return false;
        if (card.Owner?.Creature?.CombatState == null) return false;
        if (card.Type != CardType.Attack && card.Type != CardType.Skill) return false;
        if (card.Keywords.Contains(Keywords.DimKeyword.Dim)) return false;
        return true;
    }

    #endregion

    #region Visuals

    private static void UpdateVisuals()
    {
        NDustRing.Instance?.Refresh();
    }

    private static void UpdateVisualsFor(Player player)
    {
        // NDustRing 目前为单实例 UI，后续若支持多玩家可传入 player 区分
        NDustRing.Instance?.Refresh();
    }

    #endregion

    #region Animation

    /// <summary>
    /// 卡牌飞入微尘动画：从抽牌堆/手牌位置飞向角色身上的微尘环绕位置。
    /// </summary>
    private static async Task PlayCardToDustAnimation(CardModel card)
    {
        if (NGame.Instance == null) return;
        if (NCombatRoom.Instance == null) return;

        var nCard = NCard.FindOnTable(card);
        if (nCard == null)
        {
            nCard = CreateCardFromDrawPile(card);
            if (nCard == null) return;
        }

        var playerNode = GetCreatureNode(card.Owner);
        if (playerNode == null) return;

        float duration = GetAnimationDuration();
        var globalUi = NRun.Instance?.GlobalUi;
        if (globalUi == null) return;

        var startPos = nCard.GlobalPosition;
        var hand = NCombatRoom.Instance.Ui.Hand;
        var playQueue = NCombatRoom.Instance.Ui.PlayQueue;

        bool isFromHand = hand.IsAncestorOf(nCard);

        if (playQueue.IsAncestorOf(nCard))
        {
            playQueue.RemoveCardFromQueueForExecution(card);
        }

        if (isFromHand)
        {
            hand.Remove(card);
            DelayedFreeNCard(nCard);
        }
        else
        {
            nCard.GetParent()?.RemoveChildSafely(nCard);
        }

        var animCard = NCard.Create(card);
        if (animCard == null) return;

        globalUi.AddChild(animCard);
        animCard.GlobalPosition = startPos;
        animCard.UpdateVisuals(PileType.None, CardPreviewMode.Normal);

        var targetPos = playerNode.VfxSpawnPosition + new Vector2(0, -80);

        float animDuration = duration * 1.5f;
        animCard.Scale = Vector2.One * 0.7f;
        animCard.Modulate = new Color(1f, 1f, 1f, 0.95f);

        var tween = animCard.CreateTween();
        if (tween != null)
        {
            tween.TweenProperty(animCard, "global_position", targetPos, animDuration)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
            tween.Parallel().TweenProperty(animCard, "scale", Vector2.One * 0.1f, animDuration)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
            tween.Parallel().TweenProperty(animCard, "modulate:a", 0.3f, animDuration * 0.7f)
                .SetEase(Tween.EaseType.In);

            await tween.AwaitFinished(animCard);
            tween.Kill();
        }

        if (GodotObject.IsInstanceValid(animCard))
        {
            animCard.Modulate = Colors.White;
            animCard.Scale = Vector2.One;
            animCard.QueueFreeSafely();
        }

        if (!isFromHand)
        {
            nCard.QueueFreeSafely();
        }
    }

    /// <summary>
    /// 延迟归还 NCard 到对象池，避免与当前帧的动画/抽牌冲突。
    /// </summary>
    private static async void DelayedFreeNCard(NCard nCard)
    {
        if (nCard == null) return;
        await Cmd.Wait(0.05f);
        if (GodotObject.IsInstanceValid(nCard) && nCard.GetParent() == null)
        {
            nCard.QueueFreeSafely();
        }
    }

    /// <summary>
    /// 卡牌从微尘飞出的动画：用于 Iterate 等将微尘移入弃牌堆的效果。
    /// </summary>
    public static async Task PlayCardFromDustAnimation(CardModel card)
    {
        if (NGame.Instance == null) return;
        if (NCombatRoom.Instance == null) return;
        if (card.Owner == null) return;

        var playerNode = GetCreatureNode(card.Owner);
        if (playerNode == null) return;

        var globalUi = NRun.Instance?.GlobalUi;
        if (globalUi == null) return;

        float duration = GetAnimationDuration();

        var startPos = playerNode.VfxSpawnPosition + new Vector2(0, -80);

        var discardPile = PileType.Discard.GetPile(card.Owner);
        Vector2 targetPos = discardPile != null
            ? PileType.Discard.GetTargetPosition(null)
            : new Vector2(NGame.Instance.GetViewportRect().Size.X * 0.7f, NGame.Instance.GetViewportRect().Size.Y * 0.7f);

        var animCard = NCard.Create(card);
        if (animCard == null) return;

        globalUi.AddChild(animCard);
        animCard.GlobalPosition = startPos;
        animCard.Scale = Vector2.One * 0.15f;
        animCard.UpdateVisuals(PileType.None, CardPreviewMode.Normal);

        var tween = animCard.CreateTween();
        if (tween != null)
        {
            tween.TweenProperty(animCard, "global_position", targetPos, duration)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
            tween.Parallel().TweenProperty(animCard, "scale", Vector2.One * 0.5f, duration)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
            tween.Parallel().TweenProperty(animCard, "modulate:a", 0f, duration * 0.8f)
                .SetEase(Tween.EaseType.In);

            await tween.AwaitFinished(animCard);
            tween.Kill();
        }

        if (GodotObject.IsInstanceValid(animCard))
        {
            animCard.Modulate = Colors.White;
            animCard.Scale = Vector2.One;
            animCard.QueueFreeSafely();
        }
    }

    /// <summary>
    /// 从抽牌堆位置创建卡牌视觉节点。
    /// </summary>
    private static NCard? CreateCardFromDrawPile(CardModel card)
    {
        var nCard = NCard.Create(card);
        if (nCard == null) return null;

        NCombatRoom.Instance!.Ui.AddChildSafely(nCard);
        nCard.UpdateVisuals(PileType.None, CardPreviewMode.Normal);

        Vector2 screenSize = NGame.Instance!.GetViewportRect().Size;
        nCard.Position = new Vector2(
            screenSize.X * 0.75f - nCard.Size.X * 0.5f,
            screenSize.Y * 0.75f - nCard.Size.Y * 0.5f
        );

        return nCard;
    }

    private static NCreature? GetCreatureNode(Player? player)
    {
        if (player?.Creature == null) return null;
        return NCombatRoom.Instance?.GetCreatureNode(player.Creature);
    }

    private static float GetAnimationDuration()
    {
        return SaveManager.Instance?.PrefsSave?.FastMode switch
        {
            FastModeType.Fast => 0.15f,
            FastModeType.Instant => 0.01f,
            _ => 0.40f,
        };
    }

    #endregion
}

/// <summary>
/// 微尘卡牌接口。
/// </summary>
public interface IDustCard
{
    bool ExhaustAfterBlockDamage => false;
    bool DontExhaustIfExhaust => false;

    bool ShouldExhaust() => false;
    /// <summary>
    /// 当这张卡被萦绕时触发。返回 true 表示卡自己处理了萦绕逻辑（如移到手牌），跳过后续默认打出。
    /// </summary>
    Task<bool> TriggerWhenLingered() => Task.FromResult(false);
    void TriggerWhenBecomeDust() { }
    void TriggerWhenNoLongerDust() { }
    void AtTurnEndIfDust() { }
    int BlockDamageIfDust() => 0;
}
