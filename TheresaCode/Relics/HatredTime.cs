using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Relics;

/// <summary>
/// 死仇时代的恨意 (HatredTime)
/// 普通遗物（Common）
/// 
/// 效果：
/// 1. 战斗开始时：给所有敌人施加 1 层死仇时代的恨意 Power。
/// 2. 当新敌人被召唤时：也给该敌人施加 1 层死仇时代的恨意 Power。
/// 
/// 对应原版 Java Blight：
/// - onCreateEnemy: ApplyPowerAction(HatredTimePower)
/// - atBattleStartPreDraw: 给所有现存敌人施加
/// 
/// 注意：C# 中没有 Blight 系统，因此用 Common Relic 实现。
/// </summary>
[Pool(typeof(TheresaRelicPool))]
public sealed class HatredTime : TheresaRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    /// <summary>
    /// 战斗开始时：给所有敌人施加 HatredTimePower
    /// </summary>
    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null) return;

        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;
        var enemies = combatState.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0) return;

        Flash();

        foreach (var enemy in enemies)
        {
            await PowerCmd.Apply<HatredTimePower>(new ThrowingPlayerChoiceContext(), enemy, 1, Owner.Creature, null);
        }

        await base.BeforeCombatStart();
    }

    /// <summary>
    /// 当新敌人被召唤/创建时：施加 HatredTimePower
    /// 对应原版 onCreateEnemy
    /// </summary>
    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (Owner?.Creature == null) return;
        if (creature.Side != CombatSide.Enemy) return;
        if (!creature.IsAlive) return;

        Flash();
        await PowerCmd.Apply<HatredTimePower>(new ThrowingPlayerChoiceContext(), creature, 1, Owner.Creature, null);
    }
}
