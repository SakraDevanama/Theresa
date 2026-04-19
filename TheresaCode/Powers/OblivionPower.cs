using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 忘却
/// 使生命值最高的敌人失去当前生命*忘却层数=X%的生命（不会低于10点）
/// 获得时直接生效，玩家回合结束后移除
/// </summary>
public class OblivionPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// Power层数发生变化后触发效果
    /// </summary>
    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // 确保是自身层数发生了变化，并且是增加的层数
        if (power != this || amount <= 0) return;

        // 确保在战斗中
        if (CombatState == null) return;

        // 找到生命值最高的敌人
        var targetEnemy = FindHighestHpEnemy();
        if (targetEnemy == null) return;

        // 如果敌方生物血量超过9亿，造成999990000伤害
        int finalDamage;
        if (targetEnemy.CurrentHp > 900_000_000)
        {
            finalDamage = 999_990_000;
        }
        else
        {
            // 计算伤害：当前生命 * 层数% (每层1%)
            int percentage = Amount;
            decimal damagePercent = percentage / 100m;
            decimal calculatedDamage = targetEnemy.CurrentHp * damagePercent;
            
            // 确保伤害不低于10点
            finalDamage = Math.Max(10, (int)calculatedDamage);
        }

        // 造成伤害
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(), 
            targetEnemy, 
            finalDamage, 
            ValueProp.Unblockable | ValueProp.Move, 
            Owner,
            cardSource
        );

        Flash(); // 触发视觉反馈
    }

    /// <summary>
    /// 找到生命值最高的敌人
    /// </summary>
    private Creature? FindHighestHpEnemy()
    {
        if (CombatState == null) return null;

        Creature? highestHpEnemy = null;
        int maxHp = -1;

        foreach (var enemy in CombatState.Enemies)
        {
            if (enemy.CurrentHp > maxHp)
            {
                maxHp = enemy.CurrentHp;
                highestHpEnemy = enemy;
            }
        }

        return highestHpEnemy;
    }

    /// <summary>
    /// 玩家回合结束后移除自身
    /// </summary>
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 确保是拥有此 Power 的生物回合结束了
        if (Owner?.Side != side) return;

        // 移除自身
        await PowerCmd.Remove(this);
    }
}
