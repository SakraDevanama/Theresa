using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 死仇时代的恨意 Power
/// 
/// 效果：
/// 1. 回合开始时：计数器-1，若归零则重置为2并给持有者施加1层恨意
/// 2. 回合结束时：更新显示层数
/// 
/// 对应原版 Java HatredTimePower：
/// - atStartOfTurn: amount--, 若 <=0 则 amount=2, 施加 HatePower(1)
/// - atEndOfTurn: showedAmount = amount
/// </summary>
public class HatredTimePower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 用于显示的层数（回合结束时更新）
    /// </summary>
    public int ShowedAmount { get; private set; } = 1;

    private class Data
    {
        public int InternalCounter = 1;
    }

    protected override object InitInternalData() => new Data();
    private Data GetData() => GetInternalData<Data>();

    public override int DisplayAmount => ShowedAmount;

    /// <summary>
    /// 回合开始时：计数器递减，归零时施加恨意
    /// </summary>
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (Owner?.Side != side) return;

        var data = GetData();
        data.InternalCounter--;

        if (data.InternalCounter <= 0)
        {
            data.InternalCounter = 2;
            Flash();
            if (Owner != null)
            {
                await PowerCmd.Apply<ZaakathHatePower>(new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null);
            }
        }

        // 更新显示
        ShowedAmount = data.InternalCounter;
        InvokeDisplayAmountChanged();
    }

    /// <summary>
    /// 回合结束时：更新显示层数
    /// </summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner?.Side != side) return;

        ShowedAmount = GetData().InternalCounter;
        InvokeDisplayAmountChanged();
    }
}
