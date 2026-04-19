using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 错位|交互 (MisalignmentInteraction) - Java原版：WrongInteraction
/// 3费攻击牌，普通稀有度
/// 
/// 效果：造成 {Damage:diff()} 点伤害。
/// 抽到手中时：如果微尘中有费用更低的牌，与其交换位置，此牌费用-1。
/// 若MantraPower大于8，则额外降低1点能耗。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class MisalignmentInteraction() : TheresaCardModel(3, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(13m, ValueProp.Move)];

    // 标记是否已经触发过抽到手中的效果（每场战斗只触发一次）
    private bool _drawEffectTriggered;

    /// <summary>
    /// 当卡牌进入战斗时注册 Drawn 事件
    /// </summary>
    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (card != this) return;
        this.Drawn += OnDrawn;
    }

    /// <summary>
    /// Drawn 事件处理器：在抽牌完成后执行交换
    /// </summary>
    private void OnDrawn()
    {
        if (_drawEffectTriggered) return;
        _ = ExecuteExchangeAsync();
    }

    private async Task ExecuteExchangeAsync()
    {
        // 等待抽牌动画完成
        await Cmd.Wait(0.5f);

        if (_drawEffectTriggered) return;
        if (Owner == null) return;
        if (CombatState == null) return;

        var hand = PileType.Hand.GetPile(Owner);
        if (!hand.Cards.Contains(this)) return;

        var dustCards = DustManager.Cards.Where(c => c.Owner == Owner).ToList();
        if (dustCards.Count == 0) return;

        var cheapestDustCard = dustCards
            .OrderBy(c => c.EnergyCost.GetResolved())
            .FirstOrDefault();

        if (cheapestDustCard == null) return;
        if (cheapestDustCard.EnergyCost.GetResolved() >= EnergyCost.GetResolved()) return;

        _drawEffectTriggered = true;

        // 获取当前 MantraPower 层数（用于额外效果判定）
        var mantraAmount = Owner.Creature.GetPowerAmount<MantraPower>();

        // 费用-1（永久减费）
        if (EnergyCost.GetResolved() > 0)
            EnergyCost.UpgradeBy(-1);
        if (mantraAmount > 8 && EnergyCost.GetResolved() > 0)
            EnergyCost.UpgradeBy(-1);

        // === 关键：正确清理视觉节点 ===
        // 1. 清理此牌在手牌中的视觉节点和 holder
        RemoveCardFromHandVisuals(this);
        // 2. 清理目标微尘牌在微尘中的视觉节点
        RemoveCardFromDustVisuals(cheapestDustCard);

        // === 数据层面交换 ===
        // 从手牌移除此牌
        this.RemoveFromCurrentPile();
        // 从微尘移除目标牌
        await DustManager.RemoveCard(cheapestDustCard);

        // 将目标牌添加到手牌（CardPileCmd.Add 会创建新的 NCard 和 holder）
        await CardPileCmd.Add(cheapestDustCard, PileType.Hand);
        // 将此牌添加到微尘
        await DustManager.AddCard(this);

        MainFile.Logger?.Info($"[MisalignmentInteraction] Exchanged with {cheapestDustCard.Id.Entry}, cost reduced");
    }

    /// <summary>
    /// 从手牌视觉中移除卡牌：找到并销毁对应的 NHandCardHolder
    /// </summary>
    private static void RemoveCardFromHandVisuals(CardModel card)
    {
        if (TestMode.IsOn) return;
        var hand = NCombatRoom.Instance?.Ui?.Hand;
        if (hand == null) return;

        // 通过 GetCardHolder 查找 holder（会检查 _selectedHandCardContainer 和 _holdersAwaitingQueue）
        var holder = hand.GetCardHolder(card);
        if (holder is NHandCardHolder handHolder)
        {
            // 使用 NPlayerHand.RemoveCardHolder 完整清理
            hand.RemoveCardHolder(handHolder);
        }
        else if (holder != null)
        {
            // 兜底：直接清理 holder
            holder.Clear();
            var parent = holder.GetParent();
            parent?.RemoveChildSafely(holder);
            holder.QueueFreeSafely();
        }
        else
        {
            // 如果找不到 holder，尝试直接找 NCard 并清理
            var nCard = NCard.FindOnTable(card);
            if (nCard != null)
            {
                var nCardParent = nCard.GetParent();
                if (nCardParent is NCardHolder cardHolder)
                {
                    cardHolder.Clear();
                    cardHolder.GetParent()?.RemoveChildSafely(cardHolder);
                    cardHolder.QueueFreeSafely();
                }
                else
                {
                    nCard.QueueFreeSafely();
                }
            }
        }
    }

    /// <summary>
    /// 从微尘视觉中移除卡牌
    /// </summary>
    private static void RemoveCardFromDustVisuals(CardModel card)
    {
        if (TestMode.IsOn) return;
        // 微尘的视觉节点由 DustManager 管理，通常不需要单独清理
        // 但如果有悬浮的 NCard，需要清理
        var nCard = NCard.FindOnTable(card);
        if (nCard != null)
        {
            nCard.QueueFreeSafely();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}
