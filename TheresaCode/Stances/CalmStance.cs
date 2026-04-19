using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using Theresa.TheresaCode.Relics;
namespace Theresa.TheresaCode.Stances;


public class CalmStance : StancePower
{
   // protected override string AuraScenePath => "res://Watcher/scenes/watcher_mod/vfx/calm_aura.tscn";
    protected override Color? BodyTint => new Color(0.7f, 0.85f, 1.3f);
    //protected override string EnterSfxPath => "res://Watcher/audio/calm_enter.ogg";
    // protected override string AmbienceLoopPath => "res://Watcher/audio/calm_loop.ogg";
    protected override Color? ScreenFlashColor => new Color(0.4f, 0.7f, 1f);

    public override Task OnExitStance(Creature creature)
    {
        var amount = 2;
        //if (creature.Player?.GetRelic<VioletLotus>() != null) amount += 1;
        if (creature.IsPlayer) creature.Player!.PlayerCombatState!.GainEnergy(amount);
        return base.OnExitStance(creature);
    }
}