using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Action;
using Theresa.TheresaCode.Minions.Nodes;

namespace Theresa.TheresaCode.Minions.Powers;

/// <summary>
/// 维什戴尔的自动攻击行动
/// 对随机敌人造成9点远程伤害
/// </summary>
public sealed class WisdelAutoAttackAction : CustomActionModel
{
    private const int AutoAttackDamage = 9;
    private const string AttackVoicePath = "res://Theresa/audio/wisdel_atk_1.wav";
    private const string Slash1SoundPath = "res://Theresa/audio/wisdel_slash1.wav";
    private const string Slash2SoundPath = "res://Theresa/audio/wisdel_slash2.wav";

    public override TargetType TargetType => TargetType.AnyEnemy;
    public override bool AutoRemoveAtTurnEnd => true;
    public override bool DecrementAfterAct => true;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target)
    {
        var actor = Owner;
        if (!actor.IsAlive) return;

        if (target == null || !target.IsAlive)
        {
            var combatState = actor.CombatState;
            if (combatState == null) return;

            var enemies = combatState.Enemies.Where(e => e.IsAlive).ToList();
            if (enemies.Count == 0) return;

            var rng = combatState.RunState?.Rng?.CombatTargets;
            target = rng != null ? rng.NextItem(enemies) : enemies[0];
            if (target == null || !target.IsAlive) return;
        }

        await PlayRangedAttackAndDamageAsync(choiceContext, actor, target);
    }

    private async Task PlayRangedAttackAndDamageAsync(PlayerChoiceContext choiceContext, Creature actor, Creature target)
    {
        PlaySound(AttackVoicePath, 0.9f);
        PlaySound(Slash1SoundPath, 1.4f);
        PlayAttackAnimation(actor);
        await Task.Delay(300);
        await CreatureCmd.Damage(choiceContext, target, AutoAttackDamage, ValueProp.Move | ValueProp.Unpowered, actor, null);
        PlaySound(Slash2SoundPath, 1.4f);
        await Task.Delay(200);
        PlayIdleAnimation(actor);
    }

    private void PlaySound(string soundPath, float volumeMultiplier = 1.0f)
    {
        try
        {
            var stream = GD.Load<AudioStream>(soundPath);
            if (stream == null) return;
            float volumeDb = 20f * (float)Math.Log10(volumeMultiplier);
            var player = new AudioStreamPlayer
            {
                Stream = stream,
                VolumeDb = volumeDb,
                PitchScale = 1f,
                Autoplay = true
            };
            if (Engine.GetMainLoop() is SceneTree sceneTree)
            {
                sceneTree.Root.AddChild(player);
                player.Finished += () => player.QueueFree();
            }
        }
        catch { }
    }

    private void PlayAttackAnimation(Creature actor)
    {
        try
        {
            var room = NCombatRoom.Instance;
            if (room == null) return;
            var nCreature = room.GetCreatureNode(actor);
            if (nCreature == null) return;
            if (nCreature.Visuals is Wisdel wisdelVisuals)
                wisdelVisuals.PlayAnimation("Attack_A", false);
        }
        catch (Exception ex)
        {
            MainFile.Logger?.Info($"[WisdelAutoAttackAction] Error playing attack animation: {ex.Message}");
        }
    }

    private void PlayIdleAnimation(Creature actor)
    {
        try
        {
            var room = NCombatRoom.Instance;
            if (room == null) return;
            var nCreature = room.GetCreatureNode(actor);
            if (nCreature == null) return;
            if (nCreature.Visuals is Wisdel wisdelVisuals)
                wisdelVisuals.PlayAnimation("Idle", true);
        }
        catch (Exception ex)
        {
            MainFile.Logger?.Info($"[WisdelAutoAttackAction] Error playing idle animation: {ex.Message}");
        }
    }
}
