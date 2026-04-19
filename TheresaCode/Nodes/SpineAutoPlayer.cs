using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Animation;

namespace Theresa.TheresaCode.Nodes;

[GlobalClass]
public partial class SpineAutoPlayer : NSpineAutoPlayer
{
	// 动画名称常量
	private const string InteractAnim = "Interact";
	private const string SpecialAnim = "Special";
	private const string IdleAnim = "Idle";

	// 动画时长（秒）
	private const double InteractDuration = 24.3;
	private const double SpecialDuration = 23.0;
	private const double IdleWaitDuration = 10.66;

	// 随机数生成器
	private RandomNumberGenerator _rng = new();

	// 状态跟踪
	private bool _hasPlayedInteract = false;
	private bool _hasPlayedSpecial = false;
	private bool _isWaiting = false;

	public override void _Ready()
	{
		_rng.Randomize();
		StartAnimationLoop();
	}

	/// <summary>
	/// 启动动画循环
	/// </summary>
	private void StartAnimationLoop()
	{
		// 重置状态
		_hasPlayedInteract = false;
		_hasPlayedSpecial = false;
		_isWaiting = false;

		// 开始随机播放
		PlayNextRandomAnimation();
	}

	/// <summary>
	/// 播放下一个随机动画（Interact 或 Special）
	/// </summary>
	private void PlayNextRandomAnimation()
	{
		if (_isWaiting) return;

		// 如果两个都播放过了，进入 Idle + 等待流程
		if (_hasPlayedInteract && _hasPlayedSpecial)
		{
			PlayIdleAndWait();
			return;
		}

		// 随机选择未播放过的动画
		List<string> availableAnims = new();
		if (!_hasPlayedInteract) availableAnims.Add(InteractAnim);
		if (!_hasPlayedSpecial) availableAnims.Add(SpecialAnim);

		string selectedAnim = availableAnims[_rng.RandiRange(0, availableAnims.Count - 1)];
		double duration = selectedAnim == InteractAnim ? InteractDuration : SpecialDuration;

		// 标记为已播放
		if (selectedAnim == InteractAnim) _hasPlayedInteract = true;
		else _hasPlayedSpecial = true;

		// 播放动画并设置定时器
		PlayAnimationOnce(selectedAnim, duration, PlayNextRandomAnimation);
	}

	/// <summary>
	/// 播放 Idle 动画并等待 10.66 秒
	/// </summary>
	private void PlayIdleAndWait()
	{
		_isWaiting = true;

		// 播放 Idle（循环）
		PlayAnimationInternal(IdleAnim, true);

		// 创建定时器等待 10.66 秒
		var timer = new Godot.Timer();
		timer.WaitTime = IdleWaitDuration;
		timer.OneShot = true;
		timer.Timeout += () =>
		{
			timer.QueueFree();
			// 重新开始循环
			CallDeferred(nameof(StartAnimationLoop));
		};
		AddChild(timer);
		timer.Start();
	}

	/// <summary>
	/// 播放单次动画并设置回调
	/// </summary>
	private void PlayAnimationOnce(string animName, double duration, Action onComplete)
	{
		PlayAnimationInternal(animName, false);

		// 创建定时器
		var timer = new Godot.Timer();
		timer.WaitTime = duration;
		timer.OneShot = true;
		timer.Timeout += () =>
		{
			timer.QueueFree();
			onComplete?.Invoke();
		};
		AddChild(timer);
		timer.Start();
	}

	/// <summary>
	/// 内部播放动画方法
	/// </summary>
	private void PlayAnimationInternal(string animationName, bool loop)
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
			megaSprite = new MegaSprite(Variant.From(parent));
		}
		catch (System.Exception ex)
		{
			GD.PushWarning($"Failed to create MegaSprite: {ex.Message}");
			return;
		}

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
			GD.PushError($"Failed to get AnimationState for '{animationName}'");
			return;
		}

		try
		{
			animState.SetAnimation(animationName, loop, 0);
			GD.Print($"[SNSpineAutoPlayer] Playing: {animationName} (Loop: {loop})");
		}
		catch (System.Exception ex)
		{
			GD.PushError($"Failed to play animation '{animationName}': {ex.Message}");
		}
	}
}
