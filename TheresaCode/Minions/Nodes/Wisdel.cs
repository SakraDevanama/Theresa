using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine; 
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Theresa.TheresaCode.Minions.Nodes;

public partial class Wisdel : NCreatureVisuals
{
    // 可以添加一个标志位，防止重复初始化
    private bool _idleAnimationSet = false;
    private bool _deathAnimationPlayed = false;
    private bool _summonAnimationPlayed = false;

    public override void _Ready()
    {
        base._Ready();
        
        // 初始不播放动画，等待召唤动画
        // SetIdleLoopAnimation();
    }

    /// <summary>
    /// 设置 Idle 循环动画
    /// </summary>
    private void SetIdleLoopAnimation()
    {
        try
        {
            // 获取 SpineSprite 节点
            var spineSprite = GetNodeOrNull<Node>("Visuals/SpineSprite"); // 使用 Node 作为通用类型
            if (spineSprite == null)
            {
                GD.PrintErr("❌ Wisdel: SpineSprite node not found!");
                return;
            }

            // 创建 MegaSprite 实例来控制 Spine 动画
            var megaSprite = new MegaSprite(Variant.From(spineSprite));

            // 获取动画状态控制器
            var animState = megaSprite.GetAnimationState();
            if (animState == null)
            {
                GD.PrintErr("❌ Wisdel: Failed to get AnimationState from MegaSprite.");
                return;
            }

            // 播放 Idle 动画并循环
            string idleAnimationName = "Idle";
            animState.SetAnimation(idleAnimationName, true, 0); // (动画名, 是否循环, 开始时间)

            GD.Print($"✅ Wisdel: Playing idle animation '{idleAnimationName}' and looping.");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"❌ Wisdel: Error playing idle animation: {ex.Message}");
        }
    }

    /// <summary>
    /// 播放指定动画
    /// </summary>
    public void PlayAnimation(string animName, bool loop)
    {
        try
        {
            var spineSprite = GetNodeOrNull("Visuals/SpineSprite");
            if (spineSprite == null) return;

            var megaSprite = new MegaSprite(Variant.From(spineSprite));
            var animState = megaSprite.GetAnimationState();
            if (animState == null) return;

            animState.SetAnimation(animName, loop, 0);
        }
        catch
        {
            // 忽略动画播放错误
            MainFile.Logger?.Info($"[Wisdel] Animation '{animName}' not found or error playing");
        }
    }

    /// <summary>
    /// 播放召唤入场动画（Start -> Idle）
    /// </summary>
    public async void PlaySummonAnimation()
    {
        if (_summonAnimationPlayed) return;
        _summonAnimationPlayed = true;

        // 播放召唤入场动画 Start
        PlayAnimation("Start", false);
        
        // 等待 Start 动画完成（约1秒）
        await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);
        
        // 切换到 Idle 循环
        PlayAnimation("Idle", true);
    }

    /// <summary>
    /// 由 NCreature 调用，开始播放死亡动画
    /// </summary>
    public void PlayDeathAnimation()
    {
        if (_deathAnimationPlayed) return;
        _deathAnimationPlayed = true;

        // 播放 Die 动画
        PlayAnimation("Die", false);
    }

    /// <summary>
    /// 隐藏 SpineSprite（在死亡动画完成后调用）
    /// </summary>
    public void HideSpineSprite()
    {
        try
        {
            var spineSprite = GetNodeOrNull("Visuals/SpineSprite");
            if (spineSprite is CanvasItem canvasItem)
            {
                canvasItem.Hide();
            }
        }
        catch
        {
            // 忽略错误
        }
    }
}
