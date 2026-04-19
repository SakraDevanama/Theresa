using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Minions.Powers;

/// <summary>
/// 余震 - 维什戴尔的专属buff
/// 攻击3次时触发，造成5点范围伤害
/// 同时会触发残影的爆炸判定
/// 注意：余震造成的伤害不会触发余震计数，防止无限循环
/// </summary>
public sealed class WisdelYuZhenPower : TheresaPowerModel
{
    private const int YuZhenDamage = 5;
    private const int AttacksNeeded = 3; // 需要攻击3次触发

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 标志位：是否正在执行余震效果，用于防止余震伤害触发余震计数
    private bool _isTriggeringYuZhen = false;

    /// <summary>
    /// 当维什戴尔造成伤害后，增加计数并检查是否触发余震
    /// 余震造成的伤害不会触发计数
    /// </summary>
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 确保是维什戴尔造成的伤害
        if (dealer != Owner) return;
        
        // 如果是余震造成的伤害，不触发计数
        if (_isTriggeringYuZhen) return;
        
        // 增加计数（使用 Power 的 Amount 来显示层数）
        await PowerCmd.Apply<WisdelYuZhenPower>(Owner, 1m, Owner, null);
        
        MainFile.Logger?.Info($"[WisdelYuZhenPower] Current amount: {Amount}");

        // 检查是否达到触发条件
        if (Amount >= AttacksNeeded)
        {
            // 触发余震（包含残影爆炸判定）
            await TriggerYuZhen(choiceContext);
            
            // 重置计数为1层（保留1层作为下一轮的基础）
            await PowerCmd.ModifyAmount(this, -2m, Owner, null);
            
            MainFile.Logger?.Info($"[WisdelYuZhenPower] Triggered and reset to 1, current amount: {Amount}");
        }
    }

    /// <summary>
    /// 触发余震效果 - 对所有敌人造成5点范围伤害
    /// 并触发所有敌人身上的残影爆炸判定
    /// 设置标志位防止余震伤害触发余震计数
    /// </summary>
    private async Task TriggerYuZhen(PlayerChoiceContext choiceContext)
    {
        if (Owner == null) return;
        
        var combatState = Owner.CombatState;
        if (combatState == null) return;

        // 设置标志位，防止余震伤害触发余震计数
        _isTriggeringYuZhen = true;

        try
        {
            // 先触发所有敌人身上的残影爆炸判定
            await CheckAllCanYingExplosion();

            // 获取所有存活的敌人
            var enemies = combatState.Enemies.Where(e => e.IsAlive).ToList();
            
            // 对所有敌人造成伤害
            foreach (var enemy in enemies)
            {
                if (enemy.IsAlive)
                {
                    await CreatureCmd.Damage(
                        choiceContext,
                        enemy,
                        YuZhenDamage,
                        ValueProp.Move | ValueProp.Unpowered,
                        Owner,
                        null
                    );
                }
            }

            // 播放视觉效果
            Flash();
        }
        finally
        {
            // 重置标志位
            _isTriggeringYuZhen = false;
        }
    }

    /// <summary>
    /// 检查所有敌人身上的残影并触发爆炸判定
    /// </summary>
    private async Task CheckAllCanYingExplosion()
    {
        if (Owner == null) return;
        
        var combatState = Owner.CombatState;
        if (combatState == null) return;

        // 获取所有存活的敌人
        var enemies = combatState.Enemies.Where(e => e.IsAlive).ToList();
        
        // 对每个有残影的敌人触发爆炸判定
        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive) continue;
            
            var canYing = enemy.Powers.FirstOrDefault(p => p is WisdelCanYingPower) as WisdelCanYingPower;
            if (canYing != null)
            {
                // 尝试触发爆炸（15%概率）
                await canYing.TryExplode(Owner);
            }
        }
    }
}
