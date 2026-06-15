using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Commands;

public static class StanceCmd
{
    public static Task EnterWrath(Creature creature, CardModel? cardSource)
    {
        return ApplyStance<WrathStance>(creature, cardSource);
    }

    public static Task EnterCalm(Creature creature, CardModel? cardSource)
    {
        return ApplyStance<CalmStance>(creature, cardSource);
    }

    public static Task EnterDivinity(Creature creature, CardModel? cardSource)
    {
        return ApplyStance<DivinityStance>(creature, cardSource);
    }

    public static Task EnterDisaster(Creature creature, CardModel? cardSource)
    {
        return ApplyStance<DisasterStance>(creature, cardSource);
    }

    public static Task ExitStance(Creature creature, CardModel? cardSource)
    {
        return ApplyStance<NoStance>(creature, cardSource);
    }

    private static async Task ApplyStance<T>(Creature creature, CardModel? cardSource) where T : StancePower
    {
        var current = creature.Powers.OfType<StancePower>().FirstOrDefault();
        var newStance = ModelDb.Power<T>();

        if (current?.GetType() == newStance?.GetType() || creature.Player == null)
            return;

        if (current != null)
        {
            await current.OnExitStance(creature);
            current.RemoveInternal();
        }

        if (newStance != null)
        {
            var applied = await PowerCmd.Apply<T>(new ThrowingPlayerChoiceContext(), creature, 1, creature, cardSource);
            if (applied != null)
                await applied.OnEnterStance(creature);
        }
    }
}
