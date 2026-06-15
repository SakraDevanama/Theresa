using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using System.Collections.Generic;
using System.Linq;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Commands;
using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Relics;

/// <summary>
/// 文学的开端 (LiteratureBegins)
/// 稀有遗物
/// 
/// 效果：
/// 1. 在每回合开始时获得1点能量。
/// 2. 战斗开始时，进入灾厄姿态。
/// 
/// Java 原版：
/// - onEquip: energyMaster++
/// - onUnequip: energyMaster--
/// - atBattleStartPreDraw: ChangeStanceAction(new Disaster())
/// </summary>
[Pool(typeof(TheresaRelicPool))]
public sealed class LiteratureBegins : TheresaRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [ 
        HoverTipFactory.FromPower<DisasterStance>(),
    ];

    // 标记是否已添加过能量上限
    private bool _energyBonusApplied;

    // 标记本战斗是否已成功进入灾厄姿态（用于 BeforeSideTurnStart 兜底）
    private bool _combatStanceApplied;

    /// <summary>
    /// 获得遗物时：每回合开始获得1点能量
    /// 对应原版 onEquip: energyMaster++
    /// </summary>
    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner != null && !_energyBonusApplied)
        {
            Owner.PlayerCombatState?.GainEnergy(1);
            _energyBonusApplied = true;
            MainFile.Logger?.Info($"[LiteratureBegins] Applied +1 energy per turn bonus");
        }
    }

    /// <summary>
    /// 遗物被移除时：清理状态
    /// 对应原版 onUnequip: energyMaster--
    /// 
    /// 注意：STS2 中能量加成通过 AfterPlayerTurnStart 手动触发，
    /// 失去遗物后 Hook 不再调用，所以不需要额外移除能量。
    /// </summary>
    public override Task AfterRemoved()
    {
        _energyBonusApplied = false;
        MainFile.Logger?.Info($"[LiteratureBegins] Relic removed, energy bonus cleared");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 玩家回合开始时获得1点能量
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner != player) return;

        Flash();
        PlayerCmd.GainEnergy(1, player).Wait();
        MainFile.Logger?.Info($"[LiteratureBegins] Gained 1 energy at turn start");

        await Task.CompletedTask;
    }

    /// <summary>
    /// 战斗开始时进入灾厄姿态
    /// 对应原版 atBattleStartPreDraw
    /// </summary>
    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null) return;

        // 每场战斗重置标记，确保兜底逻辑可用
        _combatStanceApplied = false;

        Flash();
        await StanceCmd.EnterDisaster(Owner.Creature, null);
        _combatStanceApplied = Owner.Creature.Powers.OfType<DisasterStance>().Any();
        MainFile.Logger?.Info($"[LiteratureBegins] Entered Disaster stance at combat start (applied={_combatStanceApplied}, currentStance={Owner.Creature.Powers.OfType<StancePower>().FirstOrDefault()?.GetType().Name ?? "None"})");

        await base.BeforeCombatStart();
    }

    /// <summary>
    /// 玩家回合开始前的兜底：如果战斗开始时没有成功进入灾厄姿态，则在这里补一次。
    /// 这可以规避 BeforeCombatStart 时姿态系统尚未完全就绪、或被其他来源覆盖的问题。
    /// </summary>
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (Owner?.Creature == null || side != CombatSide.Player || _combatStanceApplied)
        {
            await base.BeforeSideTurnStart(choiceContext, side, participants, combatState);
            return;
        }

        var currentStance = Owner.Creature.Powers.OfType<StancePower>().FirstOrDefault();
        if (currentStance is DisasterStance)
        {
            _combatStanceApplied = true;
            await base.BeforeSideTurnStart(choiceContext, side, participants, combatState);
            return;
        }

        Flash();
        await StanceCmd.EnterDisaster(Owner.Creature, null);
        _combatStanceApplied = Owner.Creature.Powers.OfType<DisasterStance>().Any();
        MainFile.Logger?.Info($"[LiteratureBegins] Entered Disaster stance at side turn start fallback (applied={_combatStanceApplied}, previousStance={currentStance?.GetType().Name ?? "None"})");

        await base.BeforeSideTurnStart(choiceContext, side, participants, combatState);
    }
}
