using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Enchantments;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 丝线传播 - 回合结束前处理所有丝线效果
/// 1. 有丝线的卡触发茧笼效果（攻击牌造成伤害，技能牌获得格挡）
/// 2. 有丝线的卡向相邻卡牌复制丝线（仅手牌和微尘）
/// 
/// 注意：必须在 BeforeTurnEnd 中执行，因为回合结束时手牌会被丢弃到弃牌堆
/// </summary>
public sealed class SilkSpreadPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    protected override bool IsVisibleInternal => false;
    /// <summary>
    /// 回合结束前触发：此时手牌还在手牌堆中，可以正确传播
    /// </summary>
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner?.Side != side) return;
        if (Owner?.Player == null) return;

        var player = Owner.Player;
        var combatState = Owner.CombatState;
        if (combatState == null) return;

        // 获取手牌和微尘（茧笼效果只在这两个位置触发）
        var hand = PileType.Hand.GetPile(player);
        var dustCards = DustManager.Cards.Where(c => c.Owner == player).ToList();
        MainFile.Logger?.Info($"[SilkSpreadPower] BeforeSideTurnEnd: player={player.NetId}, handCards={hand?.Cards.Count ?? 0}, dustCards=[{string.Join(", ", dustCards.Select(c => c.Id.Entry))}]");
        
        // 1. 触发茧笼效果（仅手牌和微尘中的带丝线卡牌）
        var effectCards = new List<CardModel>();
        if (hand != null) effectCards.AddRange(hand.Cards);
        effectCards.AddRange(dustCards);
        
        foreach (var card in effectCards)
        {
            if (card.Enchantment is SilkThreadEnchantment)
            {
                await TriggerCocoonEffect(card, (CombatState)combatState, choiceContext);
            }
        }

        // 2. 传播丝线（仅在手牌和微尘中传播，和原版一致）
        // 必须在手牌被丢弃前执行！
        await SpreadSilkInPile(hand);
        await SpreadSilkInDust(dustCards);
    }

    /// <summary>
    /// 在指定牌堆中传播丝线
    /// </summary>
    private static async Task SpreadSilkInPile(CardPile? pile)
    {
        if (pile == null || pile.Cards.Count < 2) return;
        
        var cards = pile.Cards.ToList();
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (card.Enchantment is not SilkThreadEnchantment) continue;

            if (i > 0)
            {
                var leftCard = cards[i - 1];
                if (leftCard.Enchantment is not SilkThreadEnchantment)
                {
                    await TryCopySilk(leftCard);
                }
            }

            if (i < cards.Count - 1)
            {
                var rightCard = cards[i + 1];
                if (rightCard.Enchantment is not SilkThreadEnchantment)
                {
                    await TryCopySilk(rightCard);
                }
            }
        }
    }

    /// <summary>
    /// 在微尘中传播丝线（首尾相连）
    /// </summary>
    private static async Task SpreadSilkInDust(List<CardModel> dustCards)
    {
        if (dustCards.Count < 2) return;
        
        for (int i = 0; i < dustCards.Count; i++)
        {
            var card = dustCards[i];
            if (card.Enchantment is not SilkThreadEnchantment) continue;

            int leftIndex = i > 0 ? i - 1 : dustCards.Count - 1;
            var leftCard = dustCards[leftIndex];
            if (leftCard != card && leftCard.Enchantment is not SilkThreadEnchantment)
            {
                await TryCopySilk(leftCard);
            }

            int rightIndex = i < dustCards.Count - 1 ? i + 1 : 0;
            var rightCard = dustCards[rightIndex];
            if (rightCard != card && rightCard != leftCard && rightCard.Enchantment is not SilkThreadEnchantment)
            {
                await TryCopySilk(rightCard);
            }
        }
    }

    /// <summary>
    /// 触发茧笼效果：攻击牌造成伤害，技能牌获得格挡
    /// </summary>
    private async Task TriggerCocoonEffect(CardModel card, CombatState combatState, PlayerChoiceContext choiceContext)
    {
        if (Owner == null) return;

        const int CocoonDamage = 3;
        const int CocoonBlock = 3;

        // 播放卡牌闪光动画
        PlayCardFlashAnimation(card);

        if (card.Type == CardType.Attack)
        {
            var enemies = combatState.HittableEnemies.ToList();
            if (enemies.Any())
            {
                var target = card.Owner?.RunState.Rng.CombatTargets.NextItem(enemies);
                MainFile.Logger?.Info($"[SilkSpreadPower] TriggerCocoonEffect: Attack card {card.Id.Entry} targeting {target?.CombatId.ToString() ?? "null"} (enemies: {string.Join(", ", enemies.Select(e => e.CombatId.ToString()))})");
                if (target != null)
                {
                    await CreatureCmd.Damage(choiceContext, target, CocoonDamage, ValueProp.Unpowered | ValueProp.Move, Owner, null);
                }
            }
        }
        else if (card.Type == CardType.Skill)
        {
            MainFile.Logger?.Info($"[SilkSpreadPower] TriggerCocoonEffect: Skill card {card.Id.Entry} gaining {CocoonBlock} block");
            await CreatureCmd.GainBlock(Owner, CocoonBlock, ValueProp.Move, null);
        }
    }

    /// <summary>
    /// 播放卡牌闪光动画 - 使用 Modulate 变化实现
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
            
            // 使用 modulate 从白色变到亮金色再变回白色
            var originalColor = Colors.White;
            var flashColor = new Color(1.5f, 1.2f, 0.5f, 1f); // 亮金色
            
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

    /// <summary>
    /// 尝试复制丝线到目标卡
    /// </summary>
    private static async Task TryCopySilk(CardModel targetCard)
    {
        if (targetCard.Owner == null) return;

        try
        {
            var prototype = ModelDb.GetById<EnchantmentModel>(ModelDb.GetId<SilkThreadEnchantment>());
            var enchantment = (EnchantmentModel)prototype.MutableClone();
            CardCmd.Enchant(enchantment, targetCard, 1);
        }
        catch (InvalidOperationException)
        {
            // 某些卡牌不能被附魔，跳过
        }
    }
}
