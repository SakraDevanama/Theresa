using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 灾厄之力 (DisasterPower)
/// 
/// 效果：
/// 1. 受到的伤害降低20%
/// 2. 回合结束时移除自身
/// 
/// Java 原版：
/// - onAttackedToChangeDamage: 伤害 ×0.8
/// - atEndOfRound: RemoveSpecificPowerAction
/// </summary>
public class DisasterPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>
    /// 受到的伤害降低20%
    /// 在计算格挡后、实际扣血前修改失去的生命值
    /// </summary>
    public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && amount > 0)
        {
            return amount * 0.8m;
        }
        return amount;
    }

    /// <summary>
    /// 回合结束时移除自身
    /// </summary>
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Side != side) return;

        MainFile.Logger?.Info($"[DisasterPower] End of turn, removing self");
        await PowerCmd.Remove(this);
    }
}
