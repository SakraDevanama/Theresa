using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace Theresa.TheresaCode.Nodes;

/// <summary>
/// 维什戴尔事件的自定义场景节点
/// 这个节点会被添加到 Portrait 中作为背景
/// 动画播放顺序：Start -> Special -> Idle(循环)
/// </summary>
public partial class NWisdelEvent : Control, ICustomEventNode, IScreenContext
{
	public Control? DefaultFocusedControl => null;

	public IScreenContext CurrentScreenContext => this;

	private EventModel? _eventModel;
	private bool _animationStarted = false;

	public void Initialize(EventModel eventModel)
	{
		_eventModel = eventModel;
		MainFile.Logger?.Info("[NWisdelEvent] Initialized with event: " + eventModel.Id.Entry);
	}

	public override void _Ready()
	{
		base._Ready();
		MainFile.Logger?.Info("[NWisdelEvent] _Ready called");

		// 设置全屏显示
		AnchorLeft = 0;
		AnchorTop = 0;
		AnchorRight = 1;
		AnchorBottom = 1;
		OffsetLeft = 0;
		OffsetTop = 0;
		OffsetRight = 0;
		OffsetBottom = 0;

		// 播放Spine动画序列：Start -> Special -> Idle(循环)
		PlayAnimationSequence();
	}

	public override void _EnterTree()
	{
		base._EnterTree();
		MainFile.Logger?.Info("[NWisdelEvent] _EnterTree called");
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		MainFile.Logger?.Info("[NWisdelEvent] _ExitTree called");
	}

	/// <summary>
	/// 播放动画序列：Start -> Special -> Idle(循环)
	/// </summary>
	private async void PlayAnimationSequence()
	{
		if (_animationStarted) return;
		_animationStarted = true;

		try
		{
			var spineSprite = GetNodeOrNull("SpineSprite");
			if (spineSprite == null)
			{
				MainFile.Logger?.Error("[NWisdelEvent] SpineSprite node not found!");
				return;
			}

			var megaSprite = new MegaSprite(Variant.From(spineSprite));
			var animState = megaSprite.GetAnimationState();
			if (animState == null)
			{
				MainFile.Logger?.Error("[NWisdelEvent] Failed to get AnimationState from MegaSprite.");
				return;
			}

			// 1. 播放 Start 动画（不循环）
			animState.SetAnimation("Start", false, 0);
			MainFile.Logger?.Info("[NWisdelEvent] Playing Start animation");

			// 等待 Start 动画完成（约1.5秒）
			await ToSignal(GetTree().CreateTimer(3), SceneTreeTimer.SignalName.Timeout);

			// 2. 播放 Special 动画（不循环）
			animState.SetAnimation("Special", false, 0);
			MainFile.Logger?.Info("[NWisdelEvent] Playing Special animation");

			// 等待 Special 动画完成（约2秒）
			await ToSignal(GetTree().CreateTimer(14.83), SceneTreeTimer.SignalName.Timeout);

			// 3. 播放 Idle 动画（循环）
			animState.SetAnimation("Idle", true, 0);
			MainFile.Logger?.Info("[NWisdelEvent] Playing Idle animation (loop)");
		}
		catch (System.Exception ex)
		{
			MainFile.Logger?.Error($"[NWisdelEvent] Failed to play animation sequence: {ex.Message}");
		}
	}
}
