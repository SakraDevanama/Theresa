using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Minions.Powers;

/// <summary>
/// 爆裂黎明充能计数器
/// 召唤维什戴尔时给予此Power，初始1层
/// 每次玩家回合开始时自动增加一层，无上限
/// </summary>
public sealed class WisdelDawnChargePower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 玩家回合开始时自动增加一层
    /// </summary>
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        // 只在玩家回合开始时触发，且召唤物必须存活
        if (side != CombatSide.Player || Owner?.Side != CombatSide.Player || !Owner.IsAlive) return;

        // 增加一层
        await PowerCmd.Apply<WisdelDawnChargePower>(Owner, 1m, Owner, null);
    }
}
