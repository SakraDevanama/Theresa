using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Animation;

namespace Theresa.TheresaCode.Nodes;

[GlobalClass]
public partial class SNSpineAutoPlayer : NSpineAutoPlayer
{
	[Export] public string AnimationName { get; set; } = "animation";
	[Export] public bool Loop { get; set; } = true;

	private int _retryCount = 0;
	private const int MaxRetries = 5;

	public override void _Ready()
	{
		PlayTargetAnimation();
	}

	private void PlayTargetAnimation()
	{
		var parent = GetParent();
		if (parent == null)
		{
			GD.PushError("SNSpineAutoPlayer has no parent!");
			return;
		}

		MegaSprite? megaSprite = null;
		try
		{
			// 尝试构造 MegaSprite 包装器（这是 STS2 原版做法）
			megaSprite = new MegaSprite(Variant.From(parent));
		}
		catch (System.Exception ex)
		{
			GD.PushWarning($"Failed to create MegaSprite: {ex.Message}");
			RetryOrFail();
			return;
		}

		// 尝试获取 AnimationState
		MegaAnimationState? animState = null;
		try
		{
			animState = megaSprite.GetAnimationState();
		}
		catch
		{
			// 可能因底层对象未就绪而抛异常
		}

		if (animState == null)
		{
			RetryOrFail();
			return;
		}

		// 播放动画
		try
		{
			animState.SetAnimation(AnimationName, Loop, 0);
		}
		catch (System.Exception ex)
		{
			GD.PushError($"Failed to play animation '{AnimationName}': {ex.Message}");
		}
	}

	/// <summary>
	/// 播放指定的动画
	/// </summary>
	/// <param name="animationName">动画名称</param>
	/// <param name="loop">是否循环播放</param>
	public void PlayAnimation(string animationName, bool loop = true)
	{
		AnimationName = animationName;
		Loop = loop;
		_retryCount = 0;
		PlayTargetAnimation();
	}

	private void RetryOrFail()
	{
		if (_retryCount < MaxRetries)
		{
			_retryCount++;
			// 延迟 1 帧后重试
			CallDeferred(nameof(PlayTargetAnimation));
		}
		else
		{
			GD.PushError("SNSpineAutoPlayer: Failed to initialize after multiple retries.");
		}
	}
}
