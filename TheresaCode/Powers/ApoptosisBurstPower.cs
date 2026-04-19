using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 凋亡爆发
/// 持续1回合
/// 将目标凋亡的削弱攻击力效果及上限翻倍
/// 回合结束时失去5点生命
/// </summary>
public class ApoptosisBurstPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// Power层数发生变化后触发效果：刷新目标凋亡的攻击力削弱效果
    /// </summary>
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // 确保是自身层数发生了变化，并且是增加的层数
        if (power != this || amount <= 0) return;

        // 确保有拥有者
        if (Owner == null) return;

        // 找到目标身上的凋亡Power，触发其效果刷新
        var apoptosisPower = Owner.Powers.FirstOrDefault(p => p is ApoptosisPower) as ApoptosisPower;
        if (apoptosisPower != null)
        {
            // 触发凋亡Power的效果刷新，使其重新计算攻击力削弱（考虑爆发加成）
            await apoptosisPower.ForceRecalculate();
        }

        Flash();
    }

    /// <summary>
    /// 回合结束时失去5点生命并移除自身
    /// </summary>
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 确保是拥有此Power的生物的回合结束了
        if (Owner?.Side != side) return;

        // 确保在战斗中
        if (CombatState == null) return;

        // 失去20点生命（使用传入的 choiceContext）
        await CreatureCmd.Damage(
            choiceContext,
            Owner,
            5,
            ValueProp.Unblockable | ValueProp.Move,
            Owner,
            null
        );

        // 移除自身（持续1回合效果）
        await PowerCmd.Remove(this);
    }
}