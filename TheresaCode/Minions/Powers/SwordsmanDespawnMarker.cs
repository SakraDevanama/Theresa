using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Minions.Powers;

/// <summary>
/// 特雷西斯移除标记
/// 在回合结束时移除召唤物
/// </summary>
public sealed class SwordsmanDespawnMarker : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    private bool _shouldRemove = false;
    protected override bool IsVisibleInternal => false;

    /// <summary>
    /// 标记准备移除（在 SwordsmanSlashAction 中调用）
    /// </summary>
    public void MarkForRemoval()
    {
        _shouldRemove = true;
        MainFile.Logger?.Info($"[SwordsmanDespawnMarker] {Owner?.Name} marked for removal");
    }

    /// <summary>
    /// 回合结束时检查是否需要移除
    /// </summary>
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        await base.AfterTurnEnd(choiceContext, side);

        // 只在敌方回合结束时移除
        if (side != CombatSide.Enemy)
        {
            return; // 不是敌方回合，不处理
        }

        if (!_shouldRemove)
        {
            return; // 还没有被标记移除
        }

        var minion = Owner;
        if (minion == null || !minion.IsAlive)
        {
            MainFile.Logger?.Info("[SwordsmanDespawnMarker] Minion is null or not alive, skipping removal");
            return;
        }

        MainFile.Logger?.Info($"[SwordsmanDespawnMarker] Removing {minion.Name} after enemy turn end");

        // 移除这个标记 power
        await PowerCmd.Remove(this);

        // 杀死召唤物（将血量设为0，触发死亡动画和移除）
        await CreatureCmd.SetMaxAndCurrentHp(minion, 0);

        MainFile.Logger?.Info($"[SwordsmanDespawnMarker] {minion.Name} removed successfully");
    }
}
