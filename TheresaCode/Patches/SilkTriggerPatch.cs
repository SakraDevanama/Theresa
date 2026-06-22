using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Enchantments;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 丝线触发补丁 - 复刻原版 Java TriggerPatch 的核心功能
///
/// 原版 Java 通过 SpirePatch 在以下时机自动触发丝线：
/// 1. 回合结束时：TriggerPatch.EndTurnPatch → SilkPatch.atTurnEnd()
/// 2. 卡牌打出后：TriggerPatch.OnUseCardActionPatch → SilkPatch.playedCard()
///
/// STS2 版本使用 Harmony 补丁实现相同功能：
/// 1. Hook.BeforeTurnEnd Postfix：触发所有丝线效果和传播（通过 ref Task __result 异步包装）
/// 2. Hook.AfterCardPlayed Postfix：触发丝线的 AfterPlayed
/// </summary>
public static class SilkTriggerPatch
{
    /// <summary>
    /// 回合结束前触发所有丝线效果和传播
    /// 对应原版 SilkPatch.atTurnEnd()
    /// 
    /// 注意：使用 ref Task __result 异步包装，避免在 Postfix 中调用 .Wait() 导致死锁
    /// 参考：ShoujoKagekiAijoKaren 的 Async.Postfix 实现
    /// </summary>
    [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeTurnEnd))]
    public static class BeforeSideTurnEndPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref Task __result, CombatState combatState, CombatSide side)
        {
            var originalTask = __result;
            __result = PostfixContinuation(originalTask, combatState, side);
        }

        private static async Task PostfixContinuation(Task originalTask, CombatState combatState, CombatSide side)
        {
            await originalTask;

            // 遍历所有当前回合的玩家（避免 Players 集合顺序不一致导致只处理部分玩家）
            var players = combatState.Players.Where(p => p.Creature?.Side == side).ToList();
            foreach (var player in players)
            {
                var hand = PileType.Hand.GetPile(player);
                var dustCards = DustManager.CardsFor(player).ToList();
                MainFile.Logger?.Info($"[SilkTriggerPatch] BeforeSideTurnEndPatch: player={player.NetId}, handCards={hand?.Cards.Count ?? 0}, dustCards=[{string.Join(", ", dustCards.Select(c => c.Id.Entry))}]");

                // 1. 触发所有丝线的回合结束效果（抽牌堆、手牌、弃牌堆、微尘）
                await TriggerSilkEffectsAtTurnEnd(combatState, player, side);

                // 2. 在手牌中传播丝线（必须按 Owner 过滤，防止联机共享牌堆导致传到队友卡上）
                if (hand != null)
                    await SpreadSilkInPile(player, hand);

                // 3. 在微尘中传播丝线
                await SpreadSilkInDust(dustCards);
            }
        }
    }

    /// <summary>
    /// 卡牌打出后触发丝线的 AfterPlayed
    /// 对应原版 SilkPatch.playedCard()
    /// </summary>
    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
    public static class AfterCardPlayedPatch
    {
        [HarmonyPostfix]
        public static void Postfix(CombatState combatState, PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var card = cardPlay.Card;
            if (card?.Enchantment is not AbstractSilkEnchantment silk) return;

            // AfterCardPlayed 不是 async 方法，但 AfterPlayed 是 async
            // 这里不能 await，但 Hook.AfterCardPlayed 的调用方也不会 await 返回值
            // 所以使用 _ 丢弃 Task，让其在后台执行
            _ = silk.AfterPlayed(choiceContext, cardPlay);
            silk.TriggeredOnce();
        }
    }

    /// <summary>
    /// 触发所有丝线的回合结束效果
    /// 对应原版 SilkPatch.atTurnEnd() 中的 triggerSilk(TriggerType.TURN_END, ...)
    /// </summary>
    private static async Task TriggerSilkEffectsAtTurnEnd(CombatState combatState, Player player, CombatSide side)
    {
        var drawPile = PileType.Draw.GetPile(player);
        var hand = PileType.Hand.GetPile(player);
        var discardPile = PileType.Discard.GetPile(player);
        var dustCards = DustManager.CardsFor(player).ToList();
        MainFile.Logger?.Info($"[SilkTriggerPatch] TriggerSilkEffectsAtTurnEnd: draw={drawPile?.Cards.Count ?? 0}, hand={hand?.Cards.Count ?? 0}, discard={discardPile?.Cards.Count ?? 0}, dust={dustCards.Count}");

        // 收集所有需要触发效果的卡牌和对应的 PileType（只取属于当前玩家的卡，
        // 避免联机共享牌堆时触发队友的丝线效果）
        var effectCards = new List<(CardModel Card, PileType PileType)>();

        if (drawPile != null)
            foreach (var c in drawPile.Cards.Where(c => c.Owner == player))
                effectCards.Add((c, PileType.Draw));

        if (hand != null)
            foreach (var c in hand.Cards.Where(c => c.Owner == player))
                effectCards.Add((c, PileType.Hand));

        if (discardPile != null)
            foreach (var c in discardPile.Cards.Where(c => c.Owner == player))
                effectCards.Add((c, PileType.Discard));

        foreach (var c in dustCards)
            effectCards.Add((c, PileType.None)); // 微尘没有标准 PileType

        foreach (var entry in effectCards)
        {
            var card = entry.Card;
            var pileType = entry.PileType;
            if (card.Enchantment is not AbstractSilkEnchantment silk) continue;

            // 检查卡牌是否有 SilkTriggers DynamicVar，支持多次触发
            // 对应 Java 原版的 silkTriggerTimes 字段
            int triggerCount = 1;
            if (card.DynamicVars != null)
            {
                foreach (var dv in card.DynamicVars)
                {
                    if (dv.Key == "SilkTriggers")
                    {
                        triggerCount = dv.Value.IntValue;
                        break;
                    }
                }
            }

            for (int t = 0; t < triggerCount; t++)
            {
                PlayCardFlashAnimation(card);

                var hookContext = new HookPlayerChoiceContext(card, player.NetId, combatState, GameActionType.Combat);
                await silk.AtTurnEnd(hookContext, pileType);
            }
        }
    }

    /// <summary>
    /// 在手牌/抽牌堆/弃牌堆中传播丝线
    /// 对应原版 SilkPatch.expandSingleSilk(group, Owner)
    /// 只传播到属于同一玩家的卡牌，避免联机共享牌堆时串到队友卡上。
    /// </summary>
    private static async Task SpreadSilkInPile(Player player, CardPile pile)
    {
        if (pile == null || pile.Cards.Count < 2) return;

        var cards = pile.Cards.Where(c => c.Owner == player).ToList();
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (card.Enchantment is not AbstractSilkEnchantment sourceSilk) continue;

            // 向左传播
            if (i > 0)
            {
                await TrySpreadSilk(sourceSilk, cards[i - 1]);
            }

            // 向右传播
            if (i < cards.Count - 1)
            {
                await TrySpreadSilk(sourceSilk, cards[i + 1]);
            }
        }
    }

    /// <summary>
    /// 在微尘中传播丝线（首尾相连）
    /// 对应原版 SilkPatch.expandDustSilk(true)
    /// </summary>
    private static async Task SpreadSilkInDust(List<CardModel> dustCards)
    {
        if (dustCards.Count < 2) return;

        for (int i = 0; i < dustCards.Count; i++)
        {
            var card = dustCards[i];
            if (card.Enchantment is not AbstractSilkEnchantment sourceSilk) continue;

            int leftIndex = i > 0 ? i - 1 : dustCards.Count - 1;
            var leftCard = dustCards[leftIndex];
            if (leftCard != card)
            {
                await TrySpreadSilk(sourceSilk, leftCard);
            }

            int rightIndex = i < dustCards.Count - 1 ? i + 1 : 0;
            var rightCard = dustCards[rightIndex];
            if (rightCard != card && rightCard != leftCard)
            {
                await TrySpreadSilk(sourceSilk, rightCard);
            }
        }
    }

    /// <summary>
    /// 尝试将丝线传播到目标卡
    /// 对应原版 SetSilkAction（mustReplace=false, canReplace=true）
    /// </summary>
    private static async Task TrySpreadSilk(AbstractSilkEnchantment sourceSilk, CardModel targetCard)
    {
        if (targetCard.Owner == null) return;

        // 关键：丝线只能传播给同一名玩家的卡牌，防止联机共享牌堆时传到队友卡上。
        if (targetCard.Owner != sourceSilk.Card?.Owner)
        {
            MainFile.Logger?.Info($"[SilkTriggerPatch] TrySpreadSilk: skipped {targetCard.Id.Entry} because owner {targetCard.Owner.NetId} != source owner {sourceSilk.Card?.Owner.NetId ?? 0}");
            return;
        }

        // 检查源丝线是否允许传播到目标卡
        // atTurnEnd=true 表示这是回合结束时的自动传播
        if (!sourceSilk.CanSpreadAtTurnEnd(targetCard, atTurnEnd: true))
        {
            MainFile.Logger?.Info($"[SilkTriggerPatch] TrySpreadSilk: {sourceSilk.GetType().Name} cannot spread to {targetCard.Id.Entry} (has {targetCard.Enchantment?.GetType().Name ?? "null"})");
            return;
        }

        try
        {
            // 创建源丝线的副本进行传播
            var spreadSilk = (AbstractSilkEnchantment)sourceSilk.MutableClone();

            // 如果目标已有丝线且可以替换，继承数值
            var existingSilk = targetCard.Enchantment as AbstractSilkEnchantment;
            if (existingSilk != null && spreadSilk.CanReplace(existingSilk))
            {
                spreadSilk.Amount = existingSilk.Amount;
                spreadSilk.BaseAmount = existingSilk.BaseAmount;
                MainFile.Logger?.Info($"[SilkTriggerPatch] TrySpreadSilk: inheriting amount={spreadSilk.Amount}, baseAmount={spreadSilk.BaseAmount} from existing {existingSilk.GetType().Name}");
            }

            // 清除旧附魔（如果有）
            if (targetCard.Enchantment != null)
            {
                CardCmd.ClearEnchantment(targetCard);
            }

            // 附魔新丝线
            CardCmd.Enchant(spreadSilk, targetCard, spreadSilk.Amount);
            spreadSilk.ApplyPowers();
            MainFile.Logger?.Info($"[SilkTriggerPatch] TrySpreadSilk: spread {spreadSilk.GetType().Name} (amount={spreadSilk.Amount}, baseAmount={spreadSilk.BaseAmount}) to {targetCard.Id.Entry}");

            // 触发 OnCopied 回调
            spreadSilk.OnCopied();
        }
        catch (InvalidOperationException)
        {
            // 某些卡牌不能被附魔，跳过
        }
    }

    /// <summary>
    /// 播放卡牌闪光动画
    /// </summary>
    private static void PlayCardFlashAnimation(CardModel card)
    {
        if (card == null) return;

        var nCard = NCard.FindOnTable(card);
        if (nCard == null) return;

        var node = (Node)nCard;
        if (!node.IsInsideTree()) return;

        try
        {
            var tween = node.CreateTween();
            if (tween == null) return;

            var originalColor = Colors.White;
            var flashColor = new Color(1.5f, 1.2f, 0.5f, 1f);

            tween.TweenProperty(nCard, "modulate", flashColor, 0.2f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
            tween.TweenProperty(nCard, "modulate", originalColor, 0.3f)
                .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
        }
        catch
        {
            // 动画失败不影响效果
        }
    }
}
