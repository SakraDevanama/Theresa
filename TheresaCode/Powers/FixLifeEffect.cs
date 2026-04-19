using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 缝补生命效果 - 反转虚弱、脆弱、易伤的效果
/// 
/// 效果：
/// - 虚弱反转为攻击力增加 25%
/// - 脆弱反转为减伤 25%（受到伤害减少）
/// - 易伤反转为格挡增加 25%
/// 
/// 持续 1 回合，回合结束后移除
/// </summary>
public class FixLifeEffect : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;

    /// <summary>
    /// 修改伤害倍率 - 处理虚弱和脆弱效果
    /// 
    /// 虚弱反转：自己打出的伤害增加 25%（1.25 倍）
    /// 脆弱反转：自己受到的伤害减少 25%（0.75 倍）
    /// </summary>
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 情况1：自己打出的伤害（dealer == Owner）- 虚弱反转为增伤 25%
        if (dealer == Owner)
        {
            var weakPower = Owner.GetPower<WeakPower>();
            if (weakPower != null)
            {
                // 虚弱使 amount = 0.75
                // 我们想要最终倍率是 1.25（+25%）
                // 所以返回 1.25 / 0.75 = 1.666...
                return 1.6666666666666666666666666667m;
            }
        }
        
        // 情况2：自己受到的伤害（target == Owner）- 脆弱反转为减伤 25%
        if (target == Owner)
        {
            var vulnerablePower = Owner.GetPower<VulnerablePower>();
            if (vulnerablePower != null)
            {
                // 脆弱使受到伤害 amount = 1.5（+50%）
                // 我们想要最终倍率是 0.75（-25%）
                // 所以返回 0.75 / 1.5 = 0.5
                return 0.5m;
            }
        }
        
        return 1m;
    }

    /// <summary>
    /// 修改格挡倍率 - 处理易伤效果
    /// 易伤反转为格挡增加 25%：抵消易伤的 0.75 倍，然后额外 +25%
    /// 计算：基础格挡 × 0.75（易伤）→ 应该变成 基础格挡 × 1.25
    /// 所以需要返回 1.25 / 0.75 = 1.666... 来抵消易伤并 +25%
    /// 简化：直接返回 1.25（替换易伤的倍率）
    /// </summary>
    public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        // 只处理自己的格挡
        if (target != Owner) return 1m;
        
        // 检查自己是否有易伤效果
        var frailPower = Owner.GetPower<FrailPower>();
        if (frailPower == null) return 1m;
        
        // 有易伤时，返回 1.25 让格挡增加 25%
        // 注意：这个值会与当前 block 倍率相乘
        // 易伤使 block=0.75，我们希望最终是 1.25
        // 所以返回 1.25 / 0.75 = 1.666... 来抵消易伤并 +25%
        return 1.6666666666666666666666666667m;
    }

    /// <summary>
    /// 回合结束后减少层数（持续 1 回合）
    /// 在敌人回合结束后移除，确保敌人回合时的易伤/脆弱反转生效
    /// </summary>
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 敌人回合结束后减少层数
        if (side == CombatSide.Enemy && Owner?.Side == CombatSide.Player)
        {
            await PowerCmd.TickDownDuration(this);
        }
    }
}
