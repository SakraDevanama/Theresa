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
/// 维什戴尔召唤持续时间
/// 召唤时给予4层，每次玩家回合结束掉一层
/// 掉光后召唤物死亡
/// </summary>
public sealed class WisdelSummonDurationPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 玩家回合结束时减少一层
    /// </summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        // 只在玩家回合结束时触发
        if (side != CombatSide.Player || Owner?.Side != CombatSide.Player) return;

        // 减少一层
        await PowerCmd.Decrement(this);

        // 检查是否掉光
        if (Amount <= 0)
        {
            // 召唤物死亡
            if (Owner != null && Owner.IsAlive)
            {
                await CreatureCmd.Kill(Owner);
            }
        }
    }
}
