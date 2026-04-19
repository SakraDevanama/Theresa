
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 众萨卡兹的恨意：反噬
/// 受到6点伤害
/// 持续两回合
/// </summary>
public class ZaakathHateBacklashPower : TheresaPowerModel
{
    private const int DamageToTake = 6; // 反噬伤害

    // 实现基类的抽象属性
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.None; // 此效果不叠加

    // --- 核心逻辑：在拥有者所在阵营的回合结束后结算 ---
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 关键判断：确保是拥有此 Power 的生物所属的阵营回合结束了
        if (Owner.Side != side) return;

        // 检查 Amount 是否还有剩余（代表剩余回合数）
        if (Amount > 0)
        {
            // 对拥有者自己造成 6 点伤害
            if (true)
            {
                // 使用一个中性的 source 名称
                var damageVar = new DamageVar("ZAATH_HATE_BACKLASH", DamageToTake, ValueProp.Unpowered);
                
                await CreatureCmd.Damage(
                    choiceContext,      // context
                    [Owner],            // targets: 伤害目标是拥有者自己
                    damageVar,          // damageVar
                    Owner               // dealer: 伤害来源是拥有者自己
                );
            }

            // 关键：伤害执行完毕后，减少自身的 Amount (即剩余回合数)
            // 使用 PowerCmd.Decrement 来减少 Power 自身的 Amount
            // 这会导致 Amount 递减，当其变为 0 时，框架会自动移除 Power 并调用 OnRemove
            await PowerCmd.Decrement(this); // 'this' 指向当前的 Power 实例
        }
    }

    // 当 Power 因 Amount 归零或其他原因被移除时，此方法会被框架自动调用

    
}