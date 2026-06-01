using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Enchantments;

/// <summary>
/// 丝线附魔抽象基类
/// 复刻原版 Java AbstractSilk 的核心机制
/// 
/// 所有丝线类型继承此类：
/// - 茧笼 (CocoonSilk)：攻击打伤/技能获格挡
/// - 意志 (MindSilk)：获得希望/恨意
/// - 泪水 (TearSilk)：同时打伤+获格挡
/// - 愿景 (WishSilk)：微尘中萦绕并丢弃
/// - 记忆 (MemorySilk)：打出后触发记忆效果
/// </summary>
public abstract class AbstractSilkEnchantment : CustomEnchantmentModel
{
    /// <summary>
    /// 丝线基础数值（不受 SilkPower 影响）
    /// </summary>
    public virtual int BaseAmount { get; set; } = 1;

    /// <summary>
    /// 丝线当前数值（受 SilkPower 影响）
    /// </summary>
    public new virtual int Amount 
    { 
        get => _amount; 
        set => _amount = value; 
    }
    private new int _amount = 1;

    /// <summary>
    /// 是否显示数值
    /// </summary>
    public override bool ShowAmount => true;

    /// <summary>
    /// 回合结束前传播丝线时，判断是否可以覆盖目标卡的丝线
    /// 对应原版 canSpreadAtTurnEnd(cardToSpread, atTurnEnd)
    /// 
    /// 参数说明：
    /// - atTurnEnd=true: 回合结束时的自动传播（SilkSpreadPower 触发）
    /// - atTurnEnd=false: 主动传播（如叙事曲 Ballade 的效果）
    /// 
    /// 默认实现：目标无丝线 → 可以；目标有丝线 → 检查 CanReplace
    /// 子类可以覆盖此方法来区分回合结束传播和主动传播的行为
    /// </summary>
    public virtual bool CanSpreadAtTurnEnd(CardModel cardToSpread, bool atTurnEnd)
    {
        var existingSilk = cardToSpread.Enchantment as AbstractSilkEnchantment;
        if (existingSilk == null)
            return true;
        return CanReplace(existingSilk);
    }

    /// <summary>
    /// 单参数版本（默认 atTurnEnd=true，用于回合结束传播）
    /// </summary>
    public bool CanSpreadAtTurnEnd(CardModel cardToSpread)
    {
        return CanSpreadAtTurnEnd(cardToSpread, atTurnEnd: true);
    }

    /// <summary>
    /// 判断此丝线是否可以替换目标丝线
    /// 对应原版 canReplace
    /// 
    /// 替换链：
    /// - 泪水 → 可替换 → 茧笼 → 可替换 → 意志
    /// - 茧笼 → 可替换 → 意志
    /// - 意志 → 可替换 → 茧笼、泪水
    /// </summary>
    public virtual bool CanReplace(AbstractSilkEnchantment silkToReplace)
    {
        return false;
    }

    /// <summary>
    /// 判断此丝线是否可以附魔到目标卡
    /// 对应原版 canSetWhenSet
    /// </summary>
    public virtual bool CanSetWhenSet(CardModel card)
    {
        // 如果卡牌锁定了丝线（不可被替换），则任何丝线都不能附魔
        if (card is Theresa.TheresaCode.Cards.TheresaCardModel tcm && tcm.IsSilkLocked)
            return false;
        return true;
    }

    /// <summary>
    /// 应用 Power 影响，更新丝线数值
    /// 对应原版 applyPowers
    /// 
    /// SilkPower（千丝万缕）会提升所有丝线的数值
    /// </summary>
    public virtual void ApplyPowers()
    {
        if (Card?.Owner?.Creature == null) return;

        var silkPower = Card.Owner.Creature.Powers.FirstOrDefault(p => p is SilkPower) as SilkPower;
        if (silkPower != null)
        {
            Amount = BaseAmount + (int)silkPower.Amount;
        }
        else
        {
            Amount = BaseAmount;
        }
    }

    /// <summary>
    /// 丝线被复制时触发
    /// 对应原版 onCopied
    /// </summary>
    public virtual void OnCopied()
    {
    }

    /// <summary>
    /// 丝线触发一次后的回调
    /// 对应原版 triggeredOnce
    /// 可用于往昔萦绕身旁等效果
    /// </summary>
    public virtual void TriggeredOnce()
    {
    }

    /// <summary>
    /// 回合结束效果
    /// 对应原版 atTurnEnd
    /// </summary>
    public virtual async Task AtTurnEnd(PlayerChoiceContext choiceContext, PileType pileType)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// 卡牌打出后效果
    /// 对应原版 afterPlayed
    /// 
    /// 注意：此方法由 SilkTriggerPatch.AfterCardPlayedPatch 调用，
    /// 而不是由 STS2 原生的 EnchantmentModel.OnPlay 调用。
    /// 默认实现调用 OnPlay 以保持兼容性。
    /// </summary>
    public virtual async Task AfterPlayed(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        // 默认调用 OnPlay，子类可以覆盖
        await OnPlay(choiceContext, cardPlay);
    }

    /// <summary>
    /// 附魔到卡牌时的初始化
    /// </summary>
    protected override void OnEnchant()
    {
        base.OnEnchant();
        ApplyPowers();
    }

    /// <summary>
    /// 检查是否可以附魔到此卡
    /// </summary>
    public override bool CanEnchant(CardModel card)
    {
        if (!base.CanEnchant(card)) return false;
        if (!CanSetWhenSet(card)) return false;

        // 如果卡已有同类型丝线，不能再添加
        if (card.Enchantment?.GetType() == GetType()) return false;

        // 如果卡已有其他丝线，检查是否可以替换
        if (card.Enchantment is AbstractSilkEnchantment existingSilk)
        {
            return CanReplace(existingSilk);
        }

        return true;
    }
}
