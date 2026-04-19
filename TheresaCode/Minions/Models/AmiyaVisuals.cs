using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Theresa.Minions.Models;

/// <summary>
/// 阿米娅随从的视觉表现
/// </summary>
public partial class AmiyaVisuals : NCreatureVisuals
{
    private bool _deathAnimationPlayed = false;
    private bool _summonAnimationPlayed = false;

    public override void _Ready()
    {
        base._Ready();
        
        // 初始不播放动画，等待召唤动画
        // PlayAnimation("idle_loop", Owner);
    }

    /// <summary>
    /// 播放召唤入场动画（revive）
    /// </summary>
    public async void PlaySummonAnimation()
    {
        if (_summonAnimationPlayed) return;
        _summonAnimationPlayed = true;

        // 播放 revive 动画
        PlayAnimation("revive", false);
        
        // 等待 revive 动画完成（假设1.5秒）
        await ToSignal(GetTree().CreateTimer(0.9), SceneTreeTimer.SignalName.Timeout);
        
        // 切换到 idle 循环
        PlayAnimation("idle_loop", true);
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
            animState?.SetAnimation(animName, loop, 0);
        }
        catch
        {
            // 忽略动画播放错误
        }
    }

    /// <summary>
    /// 由 NCreature 调用，开始播放死亡动画
    /// </summary>
    public void PlayDeathAnimation()
    {
        if (_deathAnimationPlayed) return;
        _deathAnimationPlayed = true;

        // 播放 die 动画
        PlayAnimation("die", false);
    }

    /// <summary>
    /// 隐藏 SpineSprite（在死亡动画完成后调用）
    /// </summary>
    public void HideSpineSprite()
    {
        try
        {
            var spineSprite = GetNodeOrNull("SpineSprite");
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
