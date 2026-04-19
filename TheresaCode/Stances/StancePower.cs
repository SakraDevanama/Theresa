
using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using Theresa.TheresaCode.Extensions;
using Theresa.TheresaCode.Stances.vfx;

namespace Theresa.TheresaCode.Stances;

// 定义一个抽象基类 StancePower，所有具体姿态（如 Calm、Wrath、Divinity）都继承它
public abstract class StancePower : CustomPowerModel
{
    // 环境音效淡入/淡出时间（秒）
    private const float AmbienceFadeTime = 0.6f;

    // 环境音效的目标音量（单位：分贝，-6dB 表示略低于默认音量）
    private const float AmbienceVolume = -6f; // dB

    // ---------- 身体染色（Body Tint） ----------
    // 用于保存进入姿态前的角色原始颜色，以便退出时还原
    private static Color? _originalModulate;

    // ---------- 循环环境音效（Looping Ambience） ----------
    // 静态变量：整个游戏只允许一个环境音效播放（避免多个姿态音效叠加）
    private static AudioStreamPlayer? _ambiencePlayer;

    // ---------- 光环特效系统（Aura System） ----------
    // 当前实例的光环 VFX 节点引用
    private Node2D? _vfxInstance;

    // 所有能力都是“增益”类型（Buff）
    public override PowerType Type => PowerType.Buff;

    // 姿态能力不堆叠（只能有一种姿态生效）
    public override PowerStackType StackType => PowerStackType.None;

    // 内部隐藏：不在 UI 上显示这个能力图标（因为姿态有独立 UI）
    protected override bool IsVisibleInternal => true;

    // 虚方法：子类可重写以指定光环特效场景路径（.tscn 文件）
    protected virtual string? AuraScenePath => null;

    // 虚方法：子类可重写以设置角色身体的染色效果（null 表示无染色）
    protected virtual Color? BodyTint => null;

    // 虚方法：子类可重写以指定进入姿态时播放的音效路径
    protected virtual string? EnterSfxPath => null;

    // 虚方法：子类可重写以指定进入姿态时的屏幕闪光颜色（null 表示无闪光）
    protected virtual Color? ScreenFlashColor => null;

    // 虚方法：子类可重写以指定进入姿态时的屏幕震动强度
    protected virtual ShakeStrength ScreenShakeStrength => ShakeStrength.None;

    // 虚方法：子类可重写以指定循环播放的环境背景音效路径
    protected virtual string? AmbienceLoopPath => null;

    // 自动生成小图标路径：使用类名（去掉前缀）+ .png + 扩展方法
    public override string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();

    // 大图标复用小图标
    public override string CustomBigIconPath => CustomPackedIconPath;

    // 虚方法：当进入姿态时调用（由子类或系统触发）
    public virtual async Task OnEnterStance(Creature owner)
    {
        // 创建光环特效
        await CreateAura(owner);
        // 应用身体染色
        ApplyBodyTint(owner);
        // 播放入场音效
        PlayEnterSfx();
        // 启动循环环境音效
        StartAmbience();
        // 如果是本地玩家（即自己），才播放全屏特效（闪光+震动）
        if (LocalContext.IsMe(owner))
        {
            PlayScreenFlash();
            PlayScreenShake();
        }
    }

    // 虚方法：当退出姿态时调用
    public virtual async Task OnExitStance(Creature owner)
    {
        // 移除光环
        RemoveAura();
        // 还原身体颜色
        ResetBodyTint(owner);
        // 停止并淡出环境音效
        StopAmbience();
        // 返回已完成的任务（占位，保持 async 结构）
        await Task.CompletedTask;
    }

    // ──────────────────────────────────────────────
    //  光环特效（Aura Scene）管理
    // ──────────────────────────────────────────────

    // 创建并挂载光环特效到角色身上
    private Task CreateAura(Creature owner)
    {
        // 如果子类没提供光环路径，直接跳过
        if (AuraScenePath == null) return Task.CompletedTask;

        // 获取当前战斗房间中该生物对应的节点（Godot 场景中的表示）
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        // 获取其视觉组件（Visuals）
        var visuals = creatureNode?.Visuals;
        if (visuals == null) return Task.CompletedTask;

        // 查找或创建一个容器节点，专门用于存放姿态特效
        var container = visuals.GetNodeOrNull<Node2D>("StanceVfxContainer");
        if (container == null)
        {
            container = new Node2D { Name = "StanceVfxContainer", Position = Vector2.Zero };
            visuals.AddChild(container);
        }

        // 如果已有光环实例，先销毁它
        if (_vfxInstance != null && GodotObject.IsInstanceValid(_vfxInstance))
            _vfxInstance.QueueFree();

        // 从缓存中加载光环场景
        var scene = PreloadManager.Cache.GetScene(AuraScenePath);
        // 实例化为 Node2D
        _vfxInstance = scene.Instantiate<Node2D>();
        _vfxInstance.Position = Vector2.Zero;
        _vfxInstance.Scale = Vector2.One;
        // 添加到容器中
        container.AddChild(_vfxInstance);

        // 特殊处理：将名字包含 "Burst" 的子节点（如爆发粒子）移到角色身体图层下方，
        // 使其绘制在身体后面（避免遮挡角色）
        var bursts = _vfxInstance.GetChildren().Where(child => child.Name.ToString().Contains("Burst")).ToList();
        foreach (var burst in bursts)
        {
            // 保存全局位置
            var pos = ((Node2D)burst).GlobalPosition;
            // 重新挂载到 visuals 下
            burst.Reparent(visuals);
            // 恢复位置
            ((Node2D)burst).GlobalPosition = pos;
            // 插入到子节点列表最前面（最先绘制）
            visuals.MoveChild(burst, 0);
        }

        return Task.CompletedTask;
    }

    // 安全移除光环特效，并停止所有粒子发射器
    private void RemoveAura()
    {
        if (_vfxInstance == null || !GodotObject.IsInstanceValid(_vfxInstance)) return;

        // 遍历所有子节点，根据类型执行特定清理逻辑
        foreach (var child in _vfxInstance.GetChildren())
            switch (child)
            {
                // 愤怒姿态的火花发射器：停止生成新粒子
                case WrathGlowSparkSpawner sparks:
                    sparks.StopSpawning();
                    break;
                // 平静姿态的冰霜轨迹发射器
                case CalmFrostStreakSpawner streaks:
                    streaks.StopSpawning();
                    break;
                // 神威姿态的眼睛发射器
                case DivinityEyeSpawner eyes:
                    eyes.StopSpawning();
                    break;
                // 通用光环发射器（如气泡）
                case AuraBlobEmitter:
                {
                    // 停止所有 CPU 粒子
                    foreach (var sub in child.GetChildren())
                        if (sub is CpuParticles2D cpu)
                            cpu.Emitting = false;
                    // 2.5 秒后自动销毁该节点（等待粒子消失）
                    var timer = child.GetTree().CreateTimer(2.5f);
                    var c = child;
                    timer.Timeout += () =>
                    {
                        if (GodotObject.IsInstanceValid(c)) c.QueueFree();
                    };
                    break;
                }
            }

        // 清空引用
        _vfxInstance = null;
    }

    // ──────────────────────────────────────────────
    //  身体染色（Body Tint）管理
    // ──────────────────────────────────────────────

    // 将角色身体颜色改为指定色调
    private void ApplyBodyTint(Creature owner)
    {
        if (BodyTint == null) return;

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        if (creatureNode == null) return;

        var body = creatureNode.Body;
        // 仅在第一次进入时保存原始颜色
        _originalModulate ??= body.Modulate;
        // 应用新颜色
        body.Modulate = BodyTint.Value;
    }

    // 还原角色身体的原始颜色
    private void ResetBodyTint(Creature owner)
    {
        if (_originalModulate == null) return;

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        if (creatureNode == null) return;

        var body = creatureNode.Body;
        body.Modulate = _originalModulate.Value;
        // 清空缓存
        _originalModulate = null;
    }

    // ──────────────────────────────────────────────
    //  音效（SFX）管理
    // ──────────────────────────────────────────────

    // 播放进入姿态的音效
    private void PlayEnterSfx()
    {
        if (EnterSfxPath == null) return;
        StanceVfx.PlayStanceSfx(EnterSfxPath);
    }

    // ──────────────────────────────────────────────
    //  全屏特效（Screen Effects）管理
    // ──────────────────────────────────────────────

    // 播放屏幕闪光（仅本地玩家可见）
    private void PlayScreenFlash()
    {
        if (ScreenFlashColor == null) return;
        ScreenFlashEffect.Play(ScreenFlashColor.Value);
    }

    // 播放屏幕震动（仅本地玩家可见）
    private void PlayScreenShake()
    {
        if (ScreenShakeStrength == ShakeStrength.None) return;
        NGame.Instance?.ScreenShake(ScreenShakeStrength, ShakeDuration.Short);
    }

    // ──────────────────────────────────────────────
    //  循环环境音效（Looping Ambience）管理
    // ──────────────────────────────────────────────

    // 启动循环环境音效，并淡入
    private void StartAmbience()
    {
        if (AmbienceLoopPath == null) return;

        // 先销毁旧的音效播放器（避免叠加）
        if (_ambiencePlayer != null && GodotObject.IsInstanceValid(_ambiencePlayer))
            _ambiencePlayer.QueueFree();

        // 加载音频流
        var stream = GD.Load<AudioStream>(AmbienceLoopPath);
        if (stream == null) return;

        // 创建新的播放器
        _ambiencePlayer = new AudioStreamPlayer();
        _ambiencePlayer.Stream = stream;
        _ambiencePlayer.Bus = "SFX"; // 使用 SFX 音频总线
        _ambiencePlayer.VolumeDb = -80f; // 初始静音

        var combatRoom = NCombatRoom.Instance;
        if (combatRoom == null) return;

        // 将播放器挂到战斗房间节点下（确保随房间销毁）
        combatRoom.AddChild(_ambiencePlayer);
        _ambiencePlayer.Play();

        // 使用 Tween 动画在 0.6 秒内将音量从 -80dB 淡入到 -6dB
        var tween = _ambiencePlayer.CreateTween();
        if (tween != null)
        {
            tween.TweenProperty(_ambiencePlayer, "volume_db", AmbienceVolume, AmbienceFadeTime);
        }
    }

    // 停止环境音效，并淡出后销毁
    private static void StopAmbience()
    {
        if (_ambiencePlayer == null || !GodotObject.IsInstanceValid(_ambiencePlayer)) return;

        var player = _ambiencePlayer;
        _ambiencePlayer = null;

        // 淡出：0.6 秒内降到 -80dB
        var tween = player.CreateTween();
        if (tween != null)
        {
            tween.TweenProperty(player, "volume_db", -80f, AmbienceFadeTime);
            // 淡出完成后销毁播放器
            tween.TweenCallback(Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(player)) player.QueueFree();
            }));
        }
    }
}