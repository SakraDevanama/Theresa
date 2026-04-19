using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 编织来日效果
/// 本回合内SilkCocoon会额外触发N次
/// </summary>


public sealed class WeaveTomorrowEffect : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 内部隐藏：不在 UI 上显示这个能力图标
    protected override bool IsVisibleInternal => false;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // 确保是当前实例的层数发生变化
        if (power != this) return;

        // 如果是获得此能力，订阅SilkCocoon层数变化事件
        if (amount > 0)
        {
            // 这里我们通过拦截SilkCocoon的层数变化来实现额外触发
            // 实际上我们需要在SilkCocoon的AfterPowerAmountChanged中检测这个Power的存在
        }
    }

    /// <summary>
    /// 回合结束时移除此效果
    /// </summary>
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 确保是拥有此Power的生物所在的阵营回合结束
        if (Owner?.Side != side) return;

        // 回合结束，移除此临时效果
        if (Amount > 0)
        {
            await PowerCmd.Remove(this);
        }
    }
}
