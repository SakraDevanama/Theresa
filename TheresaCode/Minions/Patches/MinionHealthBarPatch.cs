using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MinionLib.Minion;
using MegaCrit.Sts2.Core.Logging;
using Godot;

namespace Theresa.TheresaCode.Minions.Patches;

/// <summary>
/// Fixes minion UI health bar display and interaction issues.
/// </summary>
[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class MinionHealthBarPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance)
    {
        var entity = __instance.Entity;
        if (entity == null) return;
        if (entity.Monster is not MinionModel) return;
        if (entity.PetOwner == null || !LocalContext.IsMe(entity.PetOwner)) return;

        Log.Info($"[MinionHealthBarPatch] Processing local minion: {entity.Name}");

        // Fix _isRemotePlayerOrPet field
        var isRemoteField = typeof(NCreature).GetField("_isRemotePlayerOrPet",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (isRemoteField != null)
        {
            bool currentValue = (bool)isRemoteField.GetValue(__instance)!;
            if (currentValue)
            {
                isRemoteField.SetValue(__instance, false);
                Log.Info($"[MinionHealthBarPatch] Fixed _isRemotePlayerOrPet to false");
            }
        }

        // Get _stateDisplay
        var stateDisplayField = typeof(NCreature).GetField("_stateDisplay",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (stateDisplayField == null) return;

        var stateDisplay = stateDisplayField.GetValue(__instance) as NCreatureStateDisplay;
        if (stateDisplay == null) return;

        // Kill any existing tweens
        var showHideTweenField = typeof(NCreatureStateDisplay).GetField("_showHideTween",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (showHideTweenField != null)
        {
            var existingTween = showHideTweenField.GetValue(stateDisplay) as Tween;
            existingTween?.Kill();
            showHideTweenField.SetValue(stateDisplay, null);
        }

        // Force show health bar
        stateDisplay.Visible = true;
        var modulate = stateDisplay.Modulate;
        modulate.A = 1f;
        stateDisplay.Modulate = modulate;

        // Reset position
        var originalPosField = typeof(NCreatureStateDisplay).GetField("_originalPosition",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (originalPosField != null)
        {
            var originalPos = (Vector2)originalPosField.GetValue(stateDisplay)!;
            stateDisplay.Position = originalPos;
        }

        // Force enable interaction
        __instance.ToggleIsInteractable(true);

        Log.Info($"[MinionHealthBarPatch] Fixed health bar for {entity.Name}: Visible={stateDisplay.Visible}, Modulate.A={stateDisplay.Modulate.A}");
    }
}

/// <summary>
/// Prevents NCombatRoom.AddCreature from hiding minion interactability.
/// This runs AFTER NCreature._Ready but handles the ToggleIsInteractable(false) call in AddCreature.
/// </summary>
[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature))]
public static class MinionAddCreaturePatch
{
    [HarmonyPostfix]
    public static void Postfix(NCombatRoom __instance, Creature creature)
    {
        if (creature.Monster is not MinionModel) return;
        if (creature.PetOwner == null || !LocalContext.IsMe(creature.PetOwner)) return;

        var node = __instance.GetCreatureNode(creature);
        if (node == null) return;

        // Force re-enable interactability and health bar after AddCreature completes
        node.ToggleIsInteractable(true);

        // Also ensure state display is visible
        var stateDisplayField = typeof(NCreature).GetField("_stateDisplay",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var stateDisplay = stateDisplayField?.GetValue(node) as NCreatureStateDisplay;
        if (stateDisplay != null)
        {
            stateDisplay.Visible = true;
            var modulate = stateDisplay.Modulate;
            modulate.A = 1f;
            stateDisplay.Modulate = modulate;
        }

        Log.Info($"[MinionAddCreaturePatch] Re-enabled interaction for {creature.Name}");
    }
}
