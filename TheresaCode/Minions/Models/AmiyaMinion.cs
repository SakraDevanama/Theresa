using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MinionLib.Minion;
using Theresa.TheresaCode.Minions.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Theresa.TheresaCode.Minions.Models;

/// <summary>
/// 阿米娅随从
/// 约定之王 - 提供团队增益和行动能力
/// </summary>
public sealed class AmiyaMinion : MinionModel
{
    public override int MinInitialHp => 20;

    public override int MaxInitialHp => 20;

    // 使用自定义的随从视觉资源
    protected override string VisualsPath => "res://Theresa/animations/Minion/Amiya.tscn";

    // 音效路径
    private const string TheresaFoAmiyaSoundPath = "res://Theresa/audio/Theresa_fo_Amiya.wav";
    private const string Amiya1SoundPath = "res://Theresa/audio/Amiya_1.wav";
    private const string Amiya2SoundPath = "res://Theresa/audio/Amiya_2.wav";

    // 静态缓存音效资源，避免每次召唤重新加载导致卡顿
    public static readonly Dictionary<string, AudioStream?> AudioCache = new();

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        // 定义动画状态
        // 根据你提供的动画列表: attack, attack_poke, deda_loop, die, hurt, idle_loop, revive
        
        AnimState idle = new("idle_loop", true);           // 待机循环
        AnimState attack = new("attack") { NextState = idle };  // 攻击
        AnimState attackPoke = new("attack_poke") { NextState = idle };  // 戳刺攻击
        AnimState hurt = new("hurt") { NextState = idle };  // 受伤
        AnimState die = new("die");                         // 死亡
        AnimState deadLoop = new("deda_loop", true);        // 死亡循环 (deda_loop)
        AnimState revive = new("revive") { NextState = idle };  // 复活/召唤入场

        // 设置分支 - 当受到"Hit"事件时切换到受伤动画
        idle.AddBranch("Hit", hurt);
        attack.AddBranch("Hit", hurt);
        attackPoke.AddBranch("Hit", hurt);
        hurt.AddBranch("Hit", hurt);
        
        // 死亡后进入死亡循环
        die.NextState = deadLoop;

        // 创建动画器，初始状态为 idle
        CreatureAnimator animator = new(idle, controller);
        
        // 添加任意状态转换
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("AttackPoke", attackPoke);  // 额外的攻击动画
        animator.AddAnyState("Cast", attack);            // 施法使用攻击动画
        animator.AddAnyState("Dead", die);
        animator.AddAnyState("Revive", revive);
        
        return animator;
    }

    public override async Task OnSummon(Player owner, Creature self, MinionSummonOptions options)
    {
        // 设置血量
        if (options.MaxHp is decimal maxHp)
            await CreatureCmd.SetMaxAndCurrentHp(self, maxHp);

        // 播放召唤音效（先Theresa_fo_Amiya，播放完再播放Amiya_1）
        _ = PlaySummonSoundsAsync();

        // 应用阿米娅光环能力（团队增益），持续4回合
        decimal auraDuration = options.Source?.DynamicVars["AmiyaAuraPower"].BaseValue ?? 4m;
        await PowerCmd.Apply<AmiyaAuraPower>(new ThrowingPlayerChoiceContext(), self, auraDuration, owner.Creature, options.Source);

        // 应用渐强行动能力（可执行3次）
        // 这是一个 ActionModel，玩家可以点击阿米娅来使用
        await PowerCmd.Apply<AmiyaCrescendoAction>(new ThrowingPlayerChoiceContext(), self, 3m, owner.Creature, options.Source);
    }

    /// <summary>
    /// 异步播放召唤音效（先Theresa_fo_Amiya，播放完再播放Amiya_1）
    /// </summary>
    private async Task PlaySummonSoundsAsync()
    {
        // 先播放 Theresa_fo_Amiya.wav
        await PlaySoundAsync(TheresaFoAmiyaSoundPath);
        
        // 等待一小段时间确保衔接流畅
        await Task.Delay(100);
        
        // 再播放 Amiya_1.wav
        PlaySound(Amiya1SoundPath);
    }

    /// <summary>
    /// 播放死亡音效（Amiya_2.wav）
    /// </summary>
    public void PlayDeathSound()
    {
        PlaySound(Amiya2SoundPath);
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
    /// 异步播放音效并等待完成
    /// </summary>
    private async Task PlaySoundAsync(string soundPath)
    {
        try
        {
            var stream = GetCachedAudioStream(soundPath);
            if (stream == null) return;

            var tcs = new TaskCompletionSource<bool>();

            // 创建音频播放器
            var player = new AudioStreamPlayer
            {
                Stream = stream,
                VolumeDb = 0f,
                PitchScale = 1f,
                Autoplay = true
            };

            // 添加到场景树
            if (Engine.GetMainLoop() is SceneTree sceneTree)
            {
                sceneTree.Root.AddChild(player);
            }

            // 播放完成后完成任务并释放
            player.Finished += () =>
            {
                tcs.TrySetResult(true);
                player.QueueFree();
            };

            player.Play();

            // 等待播放完成
            await tcs.Task;
        }
        catch
        {
            // 忽略音效播放错误
        }
    }

    /// <summary>
    /// 播放音效（不等待完成）
    /// </summary>
    private void PlaySound(string soundPath)
    {
        try
        {
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
