using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Theresa.TheresaCode.Monsters.Nodes;

/// <summary>
/// 特雷西斯怪物的视觉节点
/// 初始直接播放C1_Idle，攻击/蓄力使用C1_Attack
/// </summary>
public partial class TheresaSwordsmanVisuals : NCreatureVisuals
{
    private bool _deathAnimationPlayed = false;
    private bool _idleAnimationSet = false;

    public override void _Ready()
    {
        base._Ready();
        
        // 怪物没有召唤动画，初始直接播放Idle
        if (!_idleAnimationSet)
        {
            _idleAnimationSet = true;
            PlayAnimation("C1_Idle", true);
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

        PlayAnimation("C2_Skill_Die", false);
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
