using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Minions.Powers;

/// <summary>
/// 维什戴尔的自动攻击能力
/// 每回合自动对随机敌人造成5点伤害（远程攻击）
/// </summary>
public sealed class WisdelAutoAttackPower : TheresaPowerModel
{
    protected override bool IsVisibleInternal => false;    // 直接隐藏图标
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>
    /// 玩家回合开始时自动攻击（远程）——玩家本体触发
    /// </summary>
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || Owner?.Side != CombatSide.Player || !Owner.IsAlive || Owner?.Player == null) return;
        await TryAutoAttackAsync(choiceContext);
    }

    /// <summary>
    /// 玩家回合结束时自动攻击（远程）——召唤物触发
    /// </summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Owner?.Side != CombatSide.Player || !Owner.IsAlive) return;
        await TryAutoAttackAsync(choiceContext);
    }

    /// <summary>
    /// 尝试执行自动攻击
    /// </summary>
    private async Task TryAutoAttackAsync(PlayerChoiceContext choiceContext)
    {
        if (Owner == null || !Owner.IsAlive) return;

        var combatState = Owner.CombatState;
        if (combatState == null) return;

        // 获取所有存活的敌人
        var enemies = combatState.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0) return;

        // 随机选择一个敌人
        var rng = combatState.RunState?.Rng?.CombatTargets;
        var target = rng != null ? rng.NextItem(enemies) : enemies[0];
        if (target == null || !target.IsAlive) return;

        // 获取或施加自动攻击行动
        var action = Owner.GetPower<WisdelAutoAttackAction>();
        if (action == null)
        {
            var applier = Owner.PetOwner?.Creature ?? Owner;
            action = await PowerCmd.Apply<WisdelAutoAttackAction>(new ThrowingPlayerChoiceContext(), Owner, 1m, applier, null);
        }

        if (action == null) return;

        // 直接执行行动（自动攻击不需要玩家手动点击）
        await action.TryAct(choiceContext, target);
    }
}
