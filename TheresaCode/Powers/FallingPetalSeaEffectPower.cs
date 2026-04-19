using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 落零花海效果
/// 持续1回合
/// 在玩家回合开始时检查：如果上回合有能量剩余，则获得1点能量
/// </summary>
public sealed class FallingPetalSeaEffectPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 记录上回合结束时的能量
    private int _energyAtTurnEnd;

    /// <summary>
    /// 回合结束时记录剩余能量
    /// </summary>
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 确保是拥有此Power的生物的回合结束了
        if (Owner?.Side != side) return;

        // 记录当前剩余能量
        var player = Owner?.Player;
        if (player?.PlayerCombatState != null)
        {
            _energyAtTurnEnd = player.PlayerCombatState.Energy;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 能量重置时（下回合开始时）检查并奖励能量
    /// </summary>
    public override async Task AfterEnergyReset(Player player)
    {
        // 确保是拥有此Power的玩家
        if (player != Owner?.Player) return;

        // 如果上回合有能量剩余，给予5点能量
        if (_energyAtTurnEnd > 0)
        {
            Flash();
            await PlayerCmd.GainEnergy(3m, player);
        }

        // 移除自身（持续1回合的效果）
        await PowerCmd.Remove(this);
    }
}
