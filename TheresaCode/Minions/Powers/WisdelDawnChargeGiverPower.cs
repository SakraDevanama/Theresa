using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Minions.Powers;

/// <summary>
/// 爆裂黎明充能自动补充器
/// 每回合开始时自动为维什戴尔补充1层爆裂黎明充能
/// </summary>
public sealed class WisdelDawnChargeGiverPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    /// <summary>
    /// 玩家回合开始时自动补充1层充能
    /// </summary>
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, ICombatState combatState)
    {
        if (side != CombatSide.Player || Owner?.Side != CombatSide.Player || !Owner.IsAlive) return;

        await PowerCmd.Apply<WisdelDawnChargePower>(new ThrowingPlayerChoiceContext(), Owner, 1m, Owner, null);
    }
}
