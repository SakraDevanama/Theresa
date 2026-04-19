using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Enchantments;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Relics;

/// <summary>
/// 未叙魔王残片 (UntoldFragments)
/// 罕见遗物
/// 
/// 效果：
/// 在你回合结束时若有能量剩余，触发1次所有微尘的丝线。
/// 若能量不低于上限再触发1次（每次触发前重置意志的缓冲次数）。
/// 
/// Java 原版：
/// - onPlayerEndTurn: 检查能量，决定 triggerTimes
/// - TriggerDustSilkAction: 重置 MindSilk，触发所有 dustCards 的丝线效果
/// </summary>
[Pool(typeof(TheresaRelicPool))]
public sealed class UntoldFragments : TheresaRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    /// <summary>
    /// 回合结束时触发
    /// </summary>
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature == null) return;
        if (Owner.Creature?.Side != side) return;

        var player = Owner;
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;

        // 获取当前能量和能量上限
        int currentEnergy = player.PlayerCombatState?.Energy ?? 0;
        int maxEnergy = player.PlayerCombatState?.MaxEnergy ?? 3;

        // 计算触发次数
        int triggerTimes = 0;
        if (currentEnergy > 0)
        {
            triggerTimes++;
        }
        if (currentEnergy >= maxEnergy)
        {
            triggerTimes++;
        }

        if (triggerTimes == 0) return;

        MainFile.Logger?.Info($"[UntoldFragments] End of turn - Energy={currentEnergy}/{maxEnergy}, triggerTimes={triggerTimes}");

        // 触发指定次数的微尘丝线效果
        for (int i = 0; i < triggerTimes; i++)
        {
            Flash();
            await TriggerDustSilk(choiceContext, player);
            MainFile.Logger?.Info($"[UntoldFragments] Triggered dust silk {i + 1}/{triggerTimes}");
        }
    }

    /// <summary>
    /// 触发所有微尘卡牌的丝线效果
    /// 对应原版 TriggerDustSilkAction
    /// </summary>
    private static async Task TriggerDustSilk(PlayerChoiceContext choiceContext, Player player)
    {
        // 1. 重置 MindSilk 的缓冲次数
        MindSilkEnchantment.ResetPaddingRemains();
        MainFile.Logger?.Info($"[UntoldFragments] Reset MindSilk padding remains");

        // 2. 获取所有微尘卡牌
        var dustCards = DustManager.Cards.Where(c => c.Owner == player).ToList();
        if (dustCards.Count == 0)
        {
            MainFile.Logger?.Info($"[UntoldFragments] No dust cards to trigger");
            return;
        }

        // 3. 触发每张微尘卡牌的丝线效果
        foreach (var card in dustCards)
        {
            if (card.Enchantment is AbstractSilkEnchantment silk)
            {
                // 播放卡牌闪光动画
                PlayCardFlashAnimation(card);

                // 触发丝线的回合结束效果
                await silk.AtTurnEnd(choiceContext, PileType.None);
            }
        }

        // 4. 传播丝线（微尘首尾相连）
        await SpreadSilkInDust(dustCards);
    }

    /// <summary>
    /// 在微尘中传播丝线（首尾相连）
    /// 复制自 SilkSpreadPower.SpreadSilkInDust
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
    /// 复制自 SilkSpreadPower.TrySpreadSilk
    /// </summary>
    private static async Task TrySpreadSilk(AbstractSilkEnchantment sourceSilk, CardModel targetCard)
    {
        if (targetCard.Owner == null) return;

        if (!sourceSilk.CanSpreadAtTurnEnd(targetCard, atTurnEnd: true))
            return;

        try
        {
            var spreadSilk = (AbstractSilkEnchantment)sourceSilk.MutableClone();

            var existingSilk = targetCard.Enchantment as AbstractSilkEnchantment;
            if (existingSilk != null && spreadSilk.CanReplace(existingSilk))
            {
                spreadSilk.Amount = existingSilk.Amount;
                spreadSilk.BaseAmount = existingSilk.BaseAmount;
            }

            if (targetCard.Enchantment != null)
            {
                CardCmd.ClearEnchantment(targetCard);
            }

            CardCmd.Enchant(spreadSilk, targetCard, spreadSilk.Amount);
            spreadSilk.ApplyPowers();
            spreadSilk.OnCopied();
        }
        catch (InvalidOperationException)
        {
            // 某些卡牌不能被附魔，跳过
        }
    }

    /// <summary>
    /// 播放卡牌闪光动画
    /// 复制自 SilkSpreadPower.PlayCardFlashAnimation
    /// </summary>
    private static void PlayCardFlashAnimation(CardModel card)
    {
        if (card == null) return;

        var nCard = NCard.FindOnTable(card);
        if (nCard == null) return;

        var node = (Godot.Node)nCard;
        if (!node.IsInsideTree()) return;

        try
        {
            var tween = node.CreateTween();
            if (tween == null) return;

            var originalColor = Godot.Colors.White;
            var flashColor = new Godot.Color(1.5f, 1.2f, 0.5f, 1f);

            tween.TweenProperty(nCard, "modulate", flashColor, 0.2f)
                .SetEase(Godot.Tween.EaseType.Out).SetTrans(Godot.Tween.TransitionType.Sine);
            tween.TweenProperty(nCard, "modulate", originalColor, 0.3f)
                .SetEase(Godot.Tween.EaseType.InOut).SetTrans(Godot.Tween.TransitionType.Sine);
        }
        catch
        {
            // 动画失败不影响效果
        }
    }
}
