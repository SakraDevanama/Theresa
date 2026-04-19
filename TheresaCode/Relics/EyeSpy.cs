using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Relics;

/// <summary>
/// 终末之眼 (EyeSpy)
/// 普通遗物
/// 
/// 效果：
/// 1. 战斗开始时获得1层末影。
/// 2. 在每回合开始时获得1点能量。
/// 3. 若上一回合失去过生命，本回合失去2点能量。
/// 
/// Java 原版：
/// - onEquip: energyMaster++
/// - onUnequip: energyMaster--
/// - atBattleStartPreDraw: Apply EndPower(1)
/// - atTurnStart: if lost, LoseEnergy(2)
/// - wasHPLost: lost = true, beginLongPulse()
/// - onVictory: stopPulse(), lost = false
/// </summary>
[Pool(typeof(TheresaRelicPool))]
public sealed class EyeSpy : TheresaRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [ 
        HoverTipFactory.FromPower<EndShadowPower>(),
    ];
    
    
    // 标记上一回合是否失去过生命
    private bool _lostHpLastTurn;

    /// <summary>
    /// 战斗开始时：获得1层末影，重置标记
    /// </summary>
    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null) return;

        _lostHpLastTurn = false;

        Flash();
        await PowerCmd.Apply<EndShadowPower>(new ThrowingPlayerChoiceContext(), new[] { Owner.Creature }, 1, Owner.Creature, null);
        MainFile.Logger?.Info($"[EyeSpy] Applied EndShadowPower at combat start");

        await base.BeforeCombatStart();
    }

    /// <summary>
    /// 玩家回合开始时：
    /// 1. 获得1点能量
    /// 2. 如果上一回合失去过生命，失去2点能量
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner != player) return;

        // 基础效果：获得1点能量
        Flash();
        PlayerCmd.GainEnergy(1, player).Wait();
        MainFile.Logger?.Info($"[EyeSpy] Gained 1 energy at turn start");

        // 如果上一回合失去过生命，失去2点能量
        if (_lostHpLastTurn)
        {
            _lostHpLastTurn = false;
            PlayerCmd.LoseEnergy(2, player).Wait();
            MainFile.Logger?.Info($"[EyeSpy] Lost 2 energy (HP was lost last turn)");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 受到伤害后：标记失去生命
    /// 对应原版 wasHPLost
    /// </summary>
    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        // 只处理拥有者
        if (creature.Player != Owner) return;

        // delta < 0 表示失去生命
        if (delta < 0)
        {
            _lostHpLastTurn = true;
            MainFile.Logger?.Info($"[EyeSpy] HP lost this turn, marked for next turn energy penalty");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 战斗胜利后：重置标记
    /// </summary>
    public override Task AfterCombatVictory(CombatRoom room)
    {
        _lostHpLastTurn = false;
        MainFile.Logger?.Info($"[EyeSpy] Combat victory, reset HP lost marker");
        return Task.CompletedTask;
    }
}
