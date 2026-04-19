using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace Theresa.TheresaCode.Nodes;

/// <summary>
/// 阿米娅事件的自定义场景节点
/// 这个节点会被添加到 Portrait 中作为背景
/// </summary>
public partial class NAmiyaEvent : Control, ICustomEventNode, IScreenContext
{
	public Control? DefaultFocusedControl => null;

	public IScreenContext CurrentScreenContext => this;

	private EventModel? _eventModel;

	public void Initialize(EventModel eventModel)
	{
		_eventModel = eventModel;
		MainFile.Logger?.Info("[NAmiyaEvent] Initialized with event: " + eventModel.Id.Entry);
	}

	public override void _Ready()
	{
		base._Ready();
		MainFile.Logger?.Info("[NAmiyaEvent] _Ready called");

		// 设置全屏显示
		AnchorLeft = 0;
		AnchorTop = 0;
		AnchorRight = 1;
		AnchorBottom = 1;
		OffsetLeft = 0;
		OffsetTop = 0;
		OffsetRight = 0;
		OffsetBottom = 0;

		// Spine动画播放已禁用
		// var spineSpriteNode = GetNodeOrNull("SpineSprite");
		// if (spineSpriteNode != null)
		// {
		//     try
		//     {
		//         var megaSprite = new MegaSprite(spineSpriteNode);
		//         var animState = megaSprite.GetAnimationState();
		//         animState.SetAnimation("Interact", loop: true);
		//         MainFile.Logger?.Info("[NAmiyaEvent] Started Interact animation");
		//     }
		//     catch (System.Exception ex)
		//     {
		//         MainFile.Logger?.Error($"[NAmiyaEvent] Failed to play animation: {ex.Message}");
		//     }
		// }
	}

	public override void _EnterTree()
	{
		base._EnterTree();
		MainFile.Logger?.Info("[NAmiyaEvent] _EnterTree called");
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		MainFile.Logger?.Info("[NAmiyaEvent] _ExitTree called");
	}
}
