using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MinionLib.Minion;
using Theresa.TheresaCode.Minions.Cards;
using Theresa.TheresaCode.Minions.Powers;
using Theresa.TheresaCode.Minions.Interfaces;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Theresa.TheresaCode.Minions.Models;

/// <summary>
/// 维什戴尔随从
/// 自动攻击型随从，每回合自动对敌人造成9点伤害
/// </summary>
public sealed class WisdelMinion : MinionModel
{
    // 音效路径
    private const string Wisdel1SoundPath = "res://Theresa/audio/wisdel_1.wav";
    private const string Wisdel2SoundPath = "res://Theresa/audio/wisdel_2.wav";

    // 静态缓存音效资源，避免每次召唤重新加载导致卡顿
    public static readonly Dictionary<string, AudioStream?> AudioCache = new();
    public override int MinInitialHp => 25;

    public override int MaxInitialHp => 25;

    // 使用维什戴尔的Spine动画资源
    protected override string VisualsPath => "res://Theresa/animations/Minion/wisdel.tscn";

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        // 定义动画状态
        // 根据wisdel的Spine动画: Idle, Attack_A, Die, Start, Skill_2_Begin/Loop/End, Skill_3_Loop/Idle, Default
        
        AnimState idle = new("Idle", true);                         // 待机循环
        AnimState attack = new("Attack_A") { NextState = idle };    // 攻击（使用Attack_A）
        AnimState hurt = new("Default") { NextState = idle };       // 受伤（使用Default作为受伤恢复）
        AnimState die = new("Die");                                 // 死亡
        AnimState deadLoop = new("Skill_2_Idle", true);             // 死亡循环（使用Skill_2_Idle）
        AnimState summon = new("Start") { NextState = idle };       // 召唤入场动画（使用Start）
        AnimState skill3Loop = new("Skill_3_Loop") { NextState = idle };  // 爆裂黎明技能循环

        // 设置分支 - 当受到"Hit"事件时切换到受伤动画
        idle.AddBranch("Hit", hurt);
        attack.AddBranch("Hit", hurt);
        hurt.AddBranch("Hit", hurt);
        skill3Loop.AddBranch("Hit", hurt);
        
        // 死亡后进入死亡循环
        die.NextState = deadLoop;

        // 创建动画器，初始状态为 idle
        CreatureAnimator animator = new(idle, controller);
        
        // 添加任意状态转换
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", attack);
        animator.AddAnyState("Dead", die);
        animator.AddAnyState("Summon", summon);
        animator.AddAnyState("Skill3", skill3Loop);     // 触发爆裂黎明技能
        
        return animator;
    }

    public override async Task OnSummon(PlayerChoiceContext choiceContext, Player owner, MinionSummonOptions options)
    {
        // 设置血量
        if (options.MaxHp is decimal maxHp)
            await CreatureCmd.SetMaxAndCurrentHp(this.Creature, maxHp);

        // 播放随机召唤音效（wisdel_1.wav 或 wisdel_2.wav）
        PlaySummonSound();

        // 应用自动攻击能力：每回合自动对随机敌人造成9点伤害
        await PowerCmd.Apply<WisdelAutoAttackPower>(choiceContext, this.Creature, 1m, owner.Creature, options.Source, false);

        // 应用好礼能力：攻击时为当前目标附着残影
        await PowerCmd.Apply<WisdelHaoLiPower>(choiceContext, this.Creature, 1m, owner.Creature, options.Source, false);

        // 应用余震能力：攻击两次时触发5点范围伤害，并触发残影爆炸判定
        await PowerCmd.Apply<WisdelYuZhenPower>(choiceContext, this.Creature, 1m, owner.Creature, options.Source, false);

        // 应用召唤持续时间：召唤时给予4层，每次玩家回合结束掉一层，掉光后召唤物死亡
        await PowerCmd.Apply<WisdelSummonDurationPower>(choiceContext, this.Creature, 4m, owner.Creature, options.Source, false);

        // 应用爆裂黎明充能自动补充器：每回合开始时自动补充1层充能（隐藏图标）
        await PowerCmd.Apply<WisdelDawnChargeGiverPower>(choiceContext, this.Creature, 1m, owner.Creature, options.Source, false);

        // 给予玩家绑定维什戴尔的"延续"卡牌
        await GiveDurationBoundCard(owner);

        // 给予玩家绑定维什戴尔的"爆裂黎明"卡牌
        await GiveBurstDawnBoundCard(owner);
    }

    /// <summary>
    /// 获取缓存的音效资源，首次调用时加载
    /// </summary>
    private static AudioStream? GetCachedAudioStream(string soundPath)
    {
        if (!AudioCache.TryGetValue(soundPath, out var stream))
        {
            stream = GD.Load<AudioStream>(soundPath);
            AudioCache[soundPath] = stream;
        }
        return stream;
    }

    /// <summary>
    /// 播放随机召唤音效（wisdel_1.wav 或 wisdel_2.wav）
    /// </summary>
    private void PlaySummonSound()
    {
        try
        {
            // 随机选择音效（50%概率）
            var soundPath = new System.Random().Next(2) == 0 ? Wisdel1SoundPath : Wisdel2SoundPath;
            
            // 使用缓存的音效资源
            var stream = GetCachedAudioStream(soundPath);
            if (stream == null) return;

            // 创建音频播放器
            var player = new AudioStreamPlayer
            {
                Stream = stream,
                VolumeDb = 0f,
                PitchScale = 1f,
                Autoplay = true
            };

            // 添加到场景树并播放
            if (Engine.GetMainLoop() is SceneTree sceneTree)
            {
                sceneTree.Root.AddChild(player);
                player.Finished += () => player.QueueFree();
            }
        }
        catch
        {
            // 忽略音效播放错误
        }
    }

    /// <summary>
    /// 给予玩家绑定此随从的"延续"卡牌
    /// </summary>
    private async Task GiveDurationBoundCard(Player owner)
    {
        try
        {
            var combatState = this.Creature.CombatState;
            if (combatState == null) return;

            // 创建绑定随从的"延续"卡牌
            var card = combatState.CreateCard<WisdelDurationBoundCard>(owner);

            // 绑定到维什戴尔
            card.BindMinion(this.Creature);

            MainFile.Logger?.Info($"[WisdelMinion] Created WisdelDurationBoundCard bound to {this.Creature.Name}");

            // 加入手牌
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, null);
        }
        catch (Exception ex)
        {
            MainFile.Logger?.Info($"[WisdelMinion] Error giving WisdelDurationBoundCard: {ex.Message}");
        }
    }

    /// <summary>
    /// 给予玩家绑定此随从的"爆裂黎明"卡牌
    /// </summary>
    private async Task GiveBurstDawnBoundCard(Player owner)
    {
        try
        {
            var combatState = this.Creature.CombatState;
            if (combatState == null) return;

            // 创建绑定随从的"爆裂黎明"卡牌
            var card = combatState.CreateCard<BurstDawnCard>(owner);

            // 绑定到维什戴尔
            card.BindMinion(this.Creature);

            MainFile.Logger?.Info($"[WisdelMinion] Created BurstDawnCard bound to {this.Creature.Name}");

            // 加入手牌
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, null);
        }
        catch (Exception ex)
        {
            MainFile.Logger?.Info($"[WisdelMinion] Error giving BurstDawnCard: {ex.Message}");
        }
    }
}
