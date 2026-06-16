using Godot;
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
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using MegaCrit.Sts2.Core.TestSupport;
using Theresa.TheresaCode.Dust.Nodes;
using Theresa.TheresaCode.Powers;
using Theresa.TheresaCode.Relics;

namespace Theresa.TheresaCode.Dust;

/// <summary>
/// 微尘管理器 - 管理环绕角色的微尘卡牌
/// </summary>
public static class DustManager
{
    public const int BaseMaxDust = 3;
    public const int MaxDustLimit = 10;

    private static readonly List<CardModel> DustCards = [];
    // 注意：本回合被萦绕的卡牌记录已移至 BabelWord 遗物中处理，避免双重记录
    private static int _maxDustModifier = 0;

    /// <summary>
    /// 当前正在执行萦绕（DustIt）的卡牌集合，用于防止递归重入。
    /// 例如「明日渺远不及」在 OnPlay 中又会调用 DustIt，若再次选中自己会导致无限递归和牌堆状态异常。
    /// </summary>
    private static readonly HashSet<CardModel> _currentlyLingering = [];

    public static int MaxDust => BaseMaxDust + _maxDustModifier;
    public static bool IsFull => DustCards.Count >= MaxDust;
    public static IReadOnlyList<CardModel> Cards => DustCards.AsReadOnly();

    /// <summary>
    /// 增加微尘上限（用于魔王传承等效果）
    /// </summary>
    public static void IncreaseMaxDust(int amount)
    {
        _maxDustModifier += amount;
        if (_maxDustModifier > MaxDustLimit - BaseMaxDust)
        {
            _maxDustModifier = MaxDustLimit - BaseMaxDust;
        }
        else if (_maxDustModifier < -BaseMaxDust)
        {
            _maxDustModifier = -BaseMaxDust;
        }
        UpdateVisuals();
    }

    public static void PreBattle()
    {
        MainFile.Logger?.Info($"[DustManager] PreBattle: clearing {_maxDustModifier} cards, resetting max dust modifier");
        DustCards.Clear();
        _currentlyLingering.Clear();
        // LingeredThisTurn 已移除，记录由 BabelWord 遗物管理
        _maxDustModifier = 0;
        UpdateVisuals();
    }

    public static void PostBattle()
    {
        MainFile.Logger?.Info($"[DustManager] PostBattle: clearing {DustCards.Count} cards");
        DustCards.Clear();
        _currentlyLingering.Clear();
        // LingeredThisTurn 已移除，记录由 BabelWord 遗物管理
        UpdateVisuals();
    }

    /// <summary>
    /// 检查同一张卡牌实例是否已经在微尘中。
    /// 使用实例引用而非 ID.Entry 判断，允许同名卡（如多张 Strike/Defend）同时存在于微尘。
    /// </summary>
    public static bool ContainsCard(CardModel card) => DustCards.Any(c => c == card);

    public static async Task AddCard(CardModel card)
    {
        await AddCardAtIndex(card, -1);
    }

    /// <summary>
    /// 添加卡牌到微尘，可超出当前 MaxDust 限制（但不超过 MaxDustLimit）
    /// 用于 SadDust 等卡牌效果
    /// </summary>
    public static async Task AddCardOverLimit(CardModel card)
    {
        if (ContainsCard(card))
        {
            MainFile.Logger?.Info($"[DustManager] AddCardOverLimit: card {card.Id.Entry} already in dust, skipping. Current dust: {string.Join(", ", DustCards.Select(c => c.Id.Entry))}");
            return;
        }
        
        // 如果已达绝对上限，将最旧的微尘牌（末尾）移回弃牌堆
        if (DustCards.Count >= MaxDustLimit)
        {
            var oldestCard = DustCards.LastOrDefault();
            if (oldestCard != null)
            {
                MainFile.Logger?.Info($"[DustManager] AddCardOverLimit: reached limit {MaxDustLimit}, removing oldest {oldestCard.Id.Entry}");
                await RemoveCard(oldestCard);
                await CardPileCmd.Add(oldestCard, PileType.Discard);
            }
        }
        
        DustCards.Add(card);
        MainFile.Logger?.Info($"[DustManager] AddCardOverLimit: added {card.Id.Entry}. Current dust: {string.Join(", ", DustCards.Select(c => c.Id.Entry))}");

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
        UpdateVisuals();
    }

    public static async Task AddCardAtIndex(CardModel card, int index)
    {
        if (ContainsCard(card))
        {
            MainFile.Logger?.Info($"[DustManager] AddCardAtIndex: card {card.Id.Entry} already in dust, skipping. Current dust: {string.Join(", ", DustCards.Select(c => c.Id.Entry))}");
            return;
        }
        
        // 如果已达当前最大微尘数量限制，将最旧的微尘牌（末尾）移回弃牌堆
        if (DustCards.Count >= MaxDust)
        {
            var oldestCard = DustCards.LastOrDefault();
            if (oldestCard != null)
            {
                MainFile.Logger?.Info($"[DustManager] AddCardAtIndex: reached max {MaxDust}, removing oldest {oldestCard.Id.Entry}");
                await RemoveCard(oldestCard);
                await CardPileCmd.Add(oldestCard, PileType.Discard);
            }
        }
        
        if (index >= 0 && index < DustCards.Count)
        {
            DustCards.Insert(index, card);
        }
        else
        {
            DustCards.Add(card);
        }
        MainFile.Logger?.Info($"[DustManager] AddCardAtIndex: added {card.Id.Entry} at index {index}. Current dust: {string.Join(", ", DustCards.Select(c => c.Id.Entry))}");

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
        // 注意：MaoCrest 等抽牌转化逻辑会自己管理 MantraPower，这里只在溢出时补充
        if (card.Owner?.Creature != null && DustCards.Count > MaxDust)
        {
            await PowerCmd.Apply<MantraPower>(new ThrowingPlayerChoiceContext(), card.Owner.Creature, 1, card.Owner.Creature, null);
        }

        UpdateVisuals();
    }

    public static async Task RemoveCard(CardModel card)
    {
        MainFile.Logger?.Info($"[DustManager] RemoveCard: removing {card.Id.Entry}. Current dust before: {string.Join(", ", DustCards.Select(c => c.Id.Entry))}");
        RemoveCardInternal(card);
        MainFile.Logger?.Info($"[DustManager] RemoveCard: removed {card.Id.Entry}. Current dust after: {string.Join(", ", DustCards.Select(c => c.Id.Entry))}");
        
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

    private static void RemoveCardInternal(CardModel card)
    {
        var toRemove = DustCards.FirstOrDefault(c => c == card || (c.Id.Entry == card.Id.Entry && c.Owner == card.Owner));
        if (toRemove == null) return;
        DustCards.Remove(toRemove);

        if (card is IDustCard dustCard)
        {
            dustCard.TriggerWhenNoLongerDust();
        }

        UpdateVisuals();
    }

    /// <summary>
    /// 强制移除一张 Dust 卡（不减少 Mantra）。用于 Mantra < 3 时的矫正机制。
    /// </summary>
    public static void ForceRemoveDust(CardModel card)
    {
        RemoveCardInternal(card);
    }

    /// <summary>
    /// 抵挡伤害
    /// </summary>
    public static int BlockDamage(int damageAmount, Creature player)
    {
        if (damageAmount <= 0 || player == null) return damageAmount;

        var cardsToExhaust = new List<CardModel>();
        int originalDamage = damageAmount;

        foreach (var card in DustCards.ToList())
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

    /// <summary>
    /// 萦绕：随机打出一张微尘牌
    /// </summary>
    public static async Task DustIt(bool toTop, bool exhaustIt)
    {
        if (DustCards.Count == 0)
        {
            MainFile.Logger?.Info("[DustManager] DustIt: no dust cards, skipping");
            return;
        }

        var playableCards = DustCards.ToList();

        var player = playableCards[0].Owner;
        if (player == null) return;

        // 安全过滤：只使用同一玩家的卡牌，并排除当前正在萦绕的卡牌，防止递归重入
        playableCards = playableCards.Where(c => c.Owner == player && !_currentlyLingering.Contains(c)).ToList();
        if (playableCards.Count == 0) return;

        var rng = player.RunState.Rng.Shuffle;
        for (int i = playableCards.Count - 1; i > 0; i--)
        {
            int j = rng.NextInt(i + 1);
            (playableCards[i], playableCards[j]) = (playableCards[j], playableCards[i]);
        }

        var lingeredCard = playableCards[0];

        // 标记该卡正在萦绕，防止递归调用 DustIt 时再次选中它
        // （例如「明日渺远不及」在 OnPlay 中调用 ProcessDustEffect，后者又会调用 DustIt）
        _currentlyLingering.Add(lingeredCard);
        try
        {
            MainFile.Logger?.Info($"[DustManager] DustIt: selected {lingeredCard.Id.Entry} from dust [{string.Join(", ", DustCards.Select(c => c.Id.Entry))}]");
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

            var combatState = player.Creature.CombatState;
            if (combatState == null) return;

            // 创建副本
            var copy = lingeredCard.CreateClone();

            // 确定目标
            Creature? target = null;
            if (copy.TargetType == TargetType.AnyEnemy)
            {
                target = player.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies);
                MainFile.Logger?.Info($"[DustManager] DustIt: target (enemy) = {target?.CombatId.ToString() ?? "null"}");
            }
            else if (copy.TargetType == TargetType.AnyAlly)
            {
                var allies = combatState.Allies.Where(c => c != null && c.IsAlive && c.IsPlayer && c != player.Creature);
                target = player.RunState.Rng.CombatTargets.NextItem(allies);
                MainFile.Logger?.Info($"[DustManager] DustIt: target (ally) = {target?.CombatId.ToString() ?? "null"}");
            }
            else if (copy.TargetType == TargetType.Self)
            {
                target = player.Creature;
                MainFile.Logger?.Info($"[DustManager] DustIt: target (self) = {target?.CombatId.ToString() ?? "null"}");
            }

            // 资源管理
            var resources = new ResourceInfo
            {
                EnergySpent = 0,
                EnergyValue = 0,
                StarsSpent = 0,
                StarValue = 0
            };

            // 本地玩家：自定义 Dust 飞出动画（从 Dust 环飞到屏幕中央，然后消散）
            if (LocalContext.IsMe(player) && !TestMode.IsOn && NCombatRoom.Instance != null)
            {
                var visualCard = NCard.Create(copy);
                if (visualCard != null)
                {
                    Vector2 startGlobalPos = NDustRing.Instance?.GetCardGlobalPosition(lingeredCard)
                        ?? NCombatRoom.Instance.GetCreatureNode(player.Creature).GlobalPosition;

                    NCombatRoom.Instance.Ui.AddToPlayContainer(visualCard);
                    visualCard.GlobalPosition = startGlobalPos;
                    visualCard.Scale = Vector2.One * 0.5f;
                    visualCard.UpdateVisuals(PileType.Play, CardPreviewMode.Normal);

                    // 飞到屏幕中央并放大
                    Vector2 targetPos = PileType.Play.GetTargetPosition(visualCard);
                    var flyTween = visualCard.CreateTween();
                    if (flyTween != null)
                    {
                        flyTween.SetParallel(true);
                        flyTween.TweenProperty(visualCard, "position", targetPos, 0.4f)
                            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
                        flyTween.TweenProperty(visualCard, "scale", Vector2.One * 1.1f, 0.4f)
                            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);

                        // 等待飞入动画完成（或节点离开树），确保 Tween 不会残留在对象池复用后的 NCard 上
                        await flyTween.AwaitFinished(visualCard);
                        flyTween.Kill();
                    }

                    // 关键修复：在 AutoPlay 之前，将 visualCard 从 PlayContainer 移除
                    // 避免 CardPileCmd.Add(copy, PileType.Play) 通过 FindOnTable 找到 visualCard
                    // 从而防止 visualCard 被 CardPileCmd 的内部逻辑修改状态
                    visualCard.GetParent()?.RemoveChildSafely(visualCard);
                    visualCard.Visible = false;

                    // 执行效果，跳过默认的 Play 堆动画
                    await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), copy, target,
                        AutoPlayType.Default, skipXCapture: false, skipCardPileVisuals: true);

                    // 恢复 visualCard 用于消散动画
                    visualCard.Visible = true;
                    NCombatRoom.Instance.Ui.AddToPlayContainer(visualCard);

                    // 消散为黑烟
                    var exhaustVfx = NExhaustVfx.Create(visualCard);
                    if (exhaustVfx != null)
                    {
                        NCombatRoom.Instance.Ui.AddChildSafely(exhaustVfx);
                    }

                    var fadeTween = visualCard.CreateTween();
                    if (fadeTween != null)
                    {
                        fadeTween.SetParallel(true);
                        fadeTween.TweenProperty(visualCard, "modulate:a", 0f, 0.3f);
                        fadeTween.TweenProperty(visualCard, "scale", Vector2.Zero, 0.3f);

                        // 等待消散动画完成（或节点离开树），然后立刻停止 Tween
                        await fadeTween.AwaitFinished(visualCard);
                        fadeTween.Kill();
                    }

                    // 归还对象池前重置 Modulate/Scale，避免下次使用时保持透明/缩放为 0
                    if (GodotObject.IsInstanceValid(visualCard))
                    {
                        visualCard.Modulate = Colors.White;
                        visualCard.Scale = Vector2.One;
                        visualCard.QueueFreeSafely();
                    }
                }
                else
                {
                    await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), copy, target);
                }
            }
            else
            {
                await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), copy, target);
            }

            // 复制牌直接从战斗中移除，不进入任何牌堆（像 Cure 一样）。
            // AutoPlay 可能已把带有 Exhaust 等关键词的副本移入消耗/弃牌堆，此时再 RemoveFromCombat 会报错，
            // 因此忽略该异常，不影响主流程。
            try
            {
                await CardPileCmd.RemoveFromCombat(copy);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("combat pile"))
            {
                MainFile.Logger?.Info($"[DustManager] DustIt: copy {copy.Id.Entry} already removed from combat piles, skipping RemoveFromCombat");
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
            _currentlyLingering.Remove(lingeredCard);
        }
    }

    public static void AtTurnEnd()
    {
        foreach (var card in DustCards.ToList())
        {
            if (card is IDustCard dustCard)
            {
                dustCard.AtTurnEndIfDust();
            }
        }
        // LingeredThisTurn 已移除，记录由 BabelWord 遗物管理
    }

    /// <summary>
    /// 通知 BabelWord 遗物记录被萦绕的卡牌
    /// </summary>
    private static void NotifyBabelWord(Player? player, CardModel card)
    {
        if (player?.Creature == null) return;

        var babelWord = player.GetRelic<BabelWord>();
        babelWord?.OnCardLingered(card);
    }

    public static async Task AtTurnStartPostDraw(Player player)
    {
        if (player?.Creature?.CombatState == null) return;

        await DustIt(false, false);
    }

    public static void ModifyMaxDust(int delta)
    {
        _maxDustModifier += delta;
        if (MaxDust > MaxDustLimit)
            _maxDustModifier = MaxDustLimit - BaseMaxDust;
        else if (MaxDust < 0)
            _maxDustModifier = -BaseMaxDust;
    }

    public static void ResetMaxDust()
    {
        _maxDustModifier = 0;
    }

    private static void UpdateVisuals()
    {
        NDustRing.Instance?.Refresh();
    }

    /// <summary>
    /// 检查卡牌是否应该转化为微尘
    /// </summary>
    public static bool ShouldBecomeDust(CardModel card)
    {
        if (IsFull) return false;
        if (card.Owner?.Creature?.CombatState == null) return false;
        if (card.Type != CardType.Attack && card.Type != CardType.Skill) return false;
        if (card.Keywords.Contains(Keywords.DimKeyword.Dim)) return false;
        return true;
    }

    // ──────────────────────────────────────────────
    //  动画系统
    // ──────────────────────────────────────────────

    /// <summary>
    /// 卡牌飞入微尘动画：从抽牌堆/手牌位置飞向角色身上的微尘环绕位置
    /// </summary>
    private static async Task PlayCardToDustAnimation(CardModel card)
    {
        if (NGame.Instance == null) return;
        if (NCombatRoom.Instance == null) return;

        var nCard = NCard.FindOnTable(card);
        if (nCard == null)
        {
            // 从抽牌堆创建：找到抽牌堆位置
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

        // 判断NCard来源，手牌中的NCard需要特殊处理
        bool isFromHand = hand.IsAncestorOf(nCard);

        // 从播放队列中移除（如果在）
        if (playQueue.IsAncestorOf(nCard))
        {
            playQueue.RemoveCardFromQueueForExecution(card);
        }

        // 从手牌中移除（如果在）
        if (isFromHand)
        {
            hand.Remove(card);
            // 手牌NCard已被从holder移除，但未被销毁。
            // 延迟归还对象池，避免与当前帧的抽牌动画冲突
            DelayedFreeNCard(nCard);
        }
        else
        {
            // 从其他位置（如抽牌堆、播放队列）移除
            nCard.GetParent()?.RemoveChildSafely(nCard);
        }

        // 创建临时副本做飞行动画
        var animCard = NCard.Create(card);
        if (animCard == null) return;

        globalUi.AddChild(animCard);
        animCard.GlobalPosition = startPos;
        animCard.UpdateVisuals(PileType.None, CardPreviewMode.Normal);

        // 目标位置：角色上方（微尘环绕区域）
        var targetPos = playerNode.VfxSpawnPosition + new Vector2(0, -80);

        // 创建飞行动画：飞向角色并缩小消失
        // 使用更长的时长、适中的起始缩放、更高的透明度，让玩家看清卡牌
        float animDuration = duration * 1.5f; // 时长增加50%
        animCard.Scale = Vector2.One * 0.7f;  // 起始缩放适中（0.7倍，像手牌大小）
        animCard.Modulate = new Color(1f, 1f, 1f, 0.95f); // 起始透明度95%
        
        var tween = animCard.CreateTween();
        if (tween != null)
        {
            tween.TweenProperty(animCard, "global_position", targetPos, animDuration)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
            tween.Parallel().TweenProperty(animCard, "scale", Vector2.One * 0.1f, animDuration)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
            tween.Parallel().TweenProperty(animCard, "modulate:a", 0.3f, animDuration * 0.7f)
                .SetEase(Tween.EaseType.In);

            // 等待动画完成（或节点离开树），然后立刻停止 Tween 并归还对象池。
            // 这可以防止 Tween 在对象池复用后仍然修改 NCard 的 Scale/Modulate，
            // 导致奖励界面等场景中卡牌不可见。
            await tween.AwaitFinished(animCard);
            tween.Kill();
        }

        if (GodotObject.IsInstanceValid(animCard))
        {
            animCard.Modulate = Colors.White;
            animCard.Scale = Vector2.One;
            animCard.QueueFreeSafely();
        }

        // 只有非手牌来源的NCard才在这里销毁
        // 手牌NCard已在上面延迟归还
        if (!isFromHand)
        {
            nCard.QueueFreeSafely();
        }
    }

    /// <summary>
    /// 延迟归还NCard到对象池，避免与当前帧的动画/抽牌冲突
    /// </summary>
    private static async void DelayedFreeNCard(NCard nCard)
    {
        if (nCard == null) return;
        // 等待一帧，确保抽牌动画和布局刷新完成
        await Cmd.Wait(0.05f);
        if (GodotObject.IsInstanceValid(nCard) && nCard.GetParent() == null)
        {
            nCard.QueueFreeSafely();
        }
    }

    /// <summary>
    /// 卡牌从微尘飞出的动画：用于 Iterate 等将微尘移入弃牌堆的效果
    /// 卡牌从微尘环绕位置飞出，飞向弃牌堆位置并消失
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

        // 起始位置：角色上方（微尘位置）
        var startPos = playerNode.VfxSpawnPosition + new Vector2(0, -80);

        // 目标位置：弃牌堆位置
        var discardPile = PileType.Discard.GetPile(card.Owner);
        Vector2 targetPos = discardPile != null 
            ? PileType.Discard.GetTargetPosition(null) 
            : new Vector2(NGame.Instance.GetViewportRect().Size.X * 0.7f, NGame.Instance.GetViewportRect().Size.Y * 0.7f);

        // 创建临时副本做飞行动画
        var animCard = NCard.Create(card);
        if (animCard == null) return;

        globalUi.AddChild(animCard);
        animCard.GlobalPosition = startPos;
        animCard.Scale = Vector2.One * 0.15f;
        animCard.UpdateVisuals(PileType.None, CardPreviewMode.Normal);

        // 创建飞行动画：从微尘位置飞向弃牌堆
        var tween = animCard.CreateTween();
        if (tween != null)
        {
            tween.TweenProperty(animCard, "global_position", targetPos, duration)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
            tween.Parallel().TweenProperty(animCard, "scale", Vector2.One * 0.5f, duration)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
            tween.Parallel().TweenProperty(animCard, "modulate:a", 0f, duration * 0.8f)
                .SetEase(Tween.EaseType.In);

            // 等待动画完成（或节点离开树），然后立刻停止 Tween 并归还对象池。
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
    /// 从抽牌堆位置创建卡牌视觉节点
    /// </summary>
    private static NCard? CreateCardFromDrawPile(CardModel card)
    {
        var nCard = NCard.Create(card);
        if (nCard == null) return null;

        NCombatRoom.Instance!.Ui.AddChildSafely(nCard);
        nCard.UpdateVisuals(PileType.None, CardPreviewMode.Normal);

        // 放到抽牌堆位置（屏幕右下方）
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
}

/// <summary>
/// 微尘卡牌接口
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
