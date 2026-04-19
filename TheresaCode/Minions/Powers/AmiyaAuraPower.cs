using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Minions.Powers;

/// <summary>
/// 阿米娅光环 - 团队增益
/// - 玩家回合结束恢复0.25%最大生命
/// - 持续4回合
/// </summary>
public sealed class AmiyaAuraPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 治疗音效路径
    private const string HealSoundPath = "res://Theresa/audio/Amiya_heal.wav";

    /// <summary>
    /// 玩家回合结束恢复0.35%最大生命，并减少持续回合
    /// </summary>
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 只在玩家回合结束时触发
        if (side != CombatSide.Player || Owner?.Side != CombatSide.Player) return;

        // 获取所有友方单位（包括玩家和随从）
        var combatState = Owner?.CombatState;
        if (combatState == null) return;

        // 获取玩家（从 combatState.Players 中获取）
        var player = combatState.Players.FirstOrDefault();
        Creature? playerCreature = null;
        if (player != null && player.Creature.IsAlive)
        {
            playerCreature = player.Creature;

            decimal healAmount = playerCreature.MaxHp * 0.035m;
            if (healAmount > 0)
            {
                await CreatureCmd.Heal(playerCreature, healAmount);
            }
        }

        // 获取所有友方随从（排除玩家，避免重复治疗）
        var allies = combatState.Creatures
            .Where(c => c.Side == CombatSide.Player && c.IsAlive && c != playerCreature)
            .ToList();

        foreach (var ally in allies)
        {
            // 恢复 0.35% 最大生命值
            decimal healAmount = ally.MaxHp * 0.035m;
            if (healAmount > 0)
            {
                await CreatureCmd.Heal(ally, healAmount);
            }
        }

        // 播放治疗音效
        PlayHealSound();

        // 减少持续回合数，归零时自动移除
        await PowerCmd.Decrement(this);
    }

    /// <summary>
    /// 播放治疗音效
    /// </summary>
    private void PlayHealSound()
    {
        try
        {
            // 加载音效资源
            var stream = GD.Load<AudioStream>(HealSoundPath);
            if (stream == null) return;

            // 创建音频播放器
            var player = new AudioStreamPlayer
            {
                Stream = stream,
                VolumeDb = 0f,  // 音量（分贝）
                PitchScale = 1f,  // 音调
                Autoplay = true
            };

            // 添加到场景树并播放
            if (Engine.GetMainLoop() is SceneTree sceneTree)
            {
                sceneTree.Root.AddChild(player);
            }

            // 播放完成后自动释放
            player.Finished += () => player.QueueFree();
        }
        catch
        {
            // 忽略音效播放错误
        }
    }
}
