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
/// 好礼 - 维什戴尔的专属buff
/// 攻击时为当前目标附着残影
/// </summary>
public sealed class WisdelHaoLiPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;
    /// <summary>
    /// 当维什戴尔造成伤害后触发，给目标附着残影
    /// 如果目标已有残影则不附着
    /// </summary>
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 确保是维什戴尔造成的伤害
        if (dealer != Owner) return;
        
        // 确保目标存活且是敌人
        if (target == null || !target.IsAlive || target.Side == CombatSide.Player) return;

        // 检查目标是否已有残影，有则不附着
        if (target.Powers.Any(p => p is WisdelCanYingPower)) return;

        // 给目标附着残影
        await PowerCmd.Apply<WisdelCanYingPower>(target, 1m, Owner, null);
    }
}

/// <summary>
/// 残影 - 被附着在敌人身上的debuff
/// 受到维什戴尔的余震影响时有15%概率爆炸
/// 爆炸对所有敌人造成3点伤害并使其晕眩一回合
/// </summary>
public sealed class WisdelCanYingPower : TheresaPowerModel
{
    private const int ExplosionDamage = 3;
    private const float ExplosionChance = 0.15f; // 15%概率

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>
    /// 被余震影响时触发爆炸判定
    /// 这个方法由余震能力调用
    /// 无论是否触发爆炸，残影都会被移除
    /// </summary>
    public async Task<bool> TryExplode(Creature wisdel)
    {
        // 检查持有者是否存活
        if (Owner == null || !Owner.IsAlive) return false;

        // 15%概率触发爆炸（使用同步的RNG确保多人联机一致）
        var rng = Owner.CombatState?.RunState?.Rng?.CombatCardGeneration;
        if (rng != null && rng.NextFloat() < ExplosionChance)
        {
            await TriggerExplosion(wisdel);
            return true;
        }
        
        // 未触发爆炸，直接移除残影
        await PowerCmd.Remove(this);
        return false;
    }

    /// <summary>
    /// 触发爆炸效果
    /// </summary>
    private async Task TriggerExplosion(Creature wisdel)
    {
        if (Owner == null) return;
        
        var combatState = Owner.CombatState;
        if (combatState == null) return;

        // 获取所有存活的敌人
        var enemies = combatState.Enemies.Where(e => e.IsAlive).ToList();
        
        // 对所有敌人造成伤害
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive)
            {
                await CreatureCmd.Damage(
                    new ThrowingPlayerChoiceContext(),
                    enemy,
                    ExplosionDamage,
                    ValueProp.Move | ValueProp.Unpowered,
                    wisdel,
                    null
                );
                
                // 施加晕眩
                await CreatureCmd.Stun(enemy);
            }
        }

        // 爆炸后移除残影
        await PowerCmd.Remove(this);
        
        // 播放爆炸视觉效果
        Flash();
    }
}
