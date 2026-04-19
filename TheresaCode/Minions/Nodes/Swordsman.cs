using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Theresa.TheresaCode.Minions.Nodes;


public partial class Swordsman : NCreatureVisuals
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
	/// 播放召唤入场动画（C1_Move -> C1_Idle）
	/// </summary>
	public async void PlaySummonAnimation()
	{
		if (_summonAnimationPlayed) return;
		_summonAnimationPlayed = true;

		// 播放召唤入场动画 C1_Move
		PlayAnimation("C1_Move", false);
		
		// 等待 C1_Move 动画完成（约1秒）
		await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);
		
		// 切换到 C1_Idle 循环
		PlayAnimation("C1_Idle", true);
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

		// 播放 C2_Skill_Die 动画
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
