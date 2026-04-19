using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Commands;

namespace Theresa.TheresaCode.Stances;

public class DivinityStance : StancePower
{
    protected override Color? BodyTint => new Color(1.1f, 0.7f, 1.4f);
    protected override Color? ScreenFlashColor => new Color(0.8f, 0.3f, 1f);

    public override async Task OnEnterStance(Creature creature)
    {
        if (creature.IsPlayer && creature.Player != null && creature.Player.PlayerCombatState != null)
        {
            creature.Player.PlayerCombatState.GainEnergy(3);
        }

        await base.OnEnterStance(creature);
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer == Owner && !props.HasFlag(ValueProp.Unpowered))
            return 2m;
        return 1m;
    }

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        ICombatState combatState)
    {
        if (side != Owner.Side) return;
        await StanceCmd.ExitStance(Owner, null);
        await base.BeforeSideTurnStart(choiceContext, side, combatState);
    }
}
