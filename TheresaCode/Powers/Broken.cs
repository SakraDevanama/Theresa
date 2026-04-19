using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Theresa.TheresaCode.Powers;

// 破碎
public sealed class Broken : TheresaPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // --- 主动逻辑：在Power层数发生变化后，立即造成伤害并移除自身 ---
    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // 确保是自身层数发生了变化，并且是新增加的层数 (可能是首次应用)
        if (power != this || amount <= 0) return;

        // 计算伤害: 30% 每层
        decimal damage = 30m * Amount;

        // 对持有者自身造成伤害
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(), // 使用临时上下文
            new[] { Owner },                   // 伤害目标是持有者自己
            damage,                           // 伤害值
            ValueProp.Unpowered,              // 伤害无特殊属性
            Owner,                            // 伤害来源是持有者自己
            null                              // 卡牌来源为空
        );

        Flash(); // 触发视觉反馈

        // 伤害造成后，立即移除自身
        await PowerCmd.Remove(this);
    }
}