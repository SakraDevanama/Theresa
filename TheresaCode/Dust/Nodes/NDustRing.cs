using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Theresa.TheresaCode.Dust.Nodes;

/// <summary>
/// 微尘环 - 显示环绕角色的微尘卡牌
/// 复刻 Java 原版的立体环绕效果，卡牌会穿过角色身体
/// </summary>
public partial class NDustRing : Node2D
{
	private const float CircleDrawScale = 0.10f;
	private const float PreviewDrawScale = 0.7f;
	private const float YOffset = -90f; // 不要动，完美数值。
	private const float OrbitPeriod = 4f;
	private const float BackDarken = 0.6f;
	private const float LerpSpeed = 10f;

	public static NDustRing? Instance { get; private set; }

	private float _rotateRate = 0f;
	private readonly List<NCard> _cards = [];
	private readonly List<Vector2> _currentPositions = [];
	private readonly List<Vector2> _currentScales = [];
	private readonly List<float> _currentRotations = [];
	private Node2D? _backContainer;
	private Node2D? _frontContainer;
	private Node2D? _previewContainer;
	private NCreature? _parentCreature;

	public override void _EnterTree()
	{
		Instance = this;
		MainFile.Logger?.Info("[NDustRing] EnterTree - Instance set.");
	}

	public override void _ExitTree()
	{
		if (Instance == this)
			Instance = null;
	}

	public void Initialize(NCreature parentCreature)
	{
		_parentCreature = parentCreature;

		// 创建背面容器，放在 Visuals 之前
		_backContainer = new Node2D();
		_backContainer.Name = "DustBackContainer";
		parentCreature.AddChild(_backContainer);
		var visualsNode = (Node?)parentCreature.Visuals;
		int visualsIndex = visualsNode?.GetIndex() ?? 0;
		parentCreature.MoveChild(_backContainer, visualsIndex);

		// 创建正面容器，放在最后
		_frontContainer = new Node2D();
		_frontContainer.Name = "DustFrontContainer";
		parentCreature.AddChild(_frontContainer);
		parentCreature.MoveChild(_frontContainer, -1);
		
		// 创建预览容器，放在 UI 顶层以确保在 Power Tooltip 之上
		var previewLayer = new CanvasLayer();
		previewLayer.Name = "DustPreviewLayer";
		previewLayer.Layer = 10; // 在 Power Tooltip 之上
		var combatRoom = NCombatRoom.Instance;
		if (combatRoom != null)
		{
			combatRoom.AddChild(previewLayer);
		}
		else
		{
			parentCreature.AddChild(previewLayer);
		}
		_previewContainer = new Node2D();
		_previewContainer.Name = "DustPreviewContainer";
		previewLayer.AddChild(_previewContainer);
	}

	public override void _Process(double delta)
	{
		_rotateRate += (float)delta / OrbitPeriod;
		while (_rotateRate >= 1f)
			_rotateRate -= 1f;

		UpdateCards((float)delta);
	}

	public Vector2? GetCardGlobalPosition(CardModel card)
	{
		int index = DustManager.Cards.ToList().IndexOf(card);
		if (index < 0 || index >= _cards.Count) return null;
		return _cards[index].GlobalPosition;
	}

	public void Refresh()
	{
		var dustCards = DustManager.Cards;
		int targetCount = dustCards.Count;

		// 清理背面容器中的旧卡牌
		if (_backContainer != null)
		{
			foreach (var child in _backContainer.GetChildren())
			{
				if (child is NCard card)
				{
					card.Model = null;
					NodePool.Free(card);
				}
			}
			while (_backContainer.GetChildCount() > 0)
				_backContainer.RemoveChild(_backContainer.GetChild(0));
		}

		// 清理正面容器中的旧卡牌
		if (_frontContainer != null)
		{
			foreach (var child in _frontContainer.GetChildren())
			{
				if (child is NCard card)
				{
					card.Model = null;
					NodePool.Free(card);
				}
			}
			while (_frontContainer.GetChildCount() > 0)
				_frontContainer.RemoveChild(_frontContainer.GetChild(0));
		}

		_cards.Clear();
		_currentPositions.Clear();
		_currentScales.Clear();
		_currentRotations.Clear();

		for (int i = 0; i < targetCount; i++)
		{
			var card = NodePool.Get<NCard>();
			card.Model = dustCards[i];
			card.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
			// 重置SelfModulate，避免对象池重用带来的状态污染
			card.SelfModulate = Colors.White;
			_cards.Add(card);
			_currentPositions.Add(Vector2.Zero);
			_currentScales.Add(Vector2.One);
			_currentRotations.Add(0f);
		}

		UpdateCards(0f);
	}

	private void UpdateCards(float delta)
	{
		var dustCards = DustManager.Cards;
		int size = dustCards.Count;
		
		// 如果 Dust 卡牌数量与缓存不一致，重新刷新
		if (size != _cards.Count)
		{
			Refresh();
			return;
		}
		
		if (size == 0 || _parentCreature == null || _backContainer == null || _frontContainer == null) return;

		bool isPreviewMode = _parentCreature.IsFocused;

		// 卡牌宽度和环绕直径，完美贴合参数。
		float cardWidth = CircleDrawScale * 250f;
		float width = 150f * 1.1f + cardWidth;
		float tmpWidth = 2f * cardWidth;
		width = Math.Max(width, tmpWidth);

		// 预览布局参数（复刻 Java 原版）
		float previewCardWidth = 1.2f * PreviewDrawScale * NCard.defaultSize.X;
		if (size > 4)
			previewCardWidth *= 0.8f;
		float previewTotalWidth = previewCardWidth * (size > 5 ? 4 : Math.Max(0, size - 1));

		Vector2 screenSize = GetViewport().GetVisibleRect().Size;

		for (int i = 0; i < size; i++)
		{
			float rRate = _rotateRate + ((float)i / size);
			while (rRate >= 1f) rRate -= 1f;

			bool isBehind = rRate >= 0.5f;

			var card = _cards[i];
			var targetParent = isBehind ? _backContainer : _frontContainer;
			if (card.GetParent() != targetParent)
			{
				card.GetParent()?.RemoveChild(card);
				targetParent?.AddChild(card);
				if (card.Model != null)
					card.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
			}

			Vector2 targetPos;
			Vector2 targetScale;
			float targetRot;
			Color targetModulate;

			if (isPreviewMode)
			{
				int idx = i;
				int line = 0;
				if (size > 4)
				{
					while (idx >= 5)
					{
						idx -= 5;
						line++;
					}
				}

				targetScale = Vector2.One * PreviewDrawScale;
				if (size > 4)
					targetScale *= 0.8f;

				float x = -0.5f * previewTotalWidth + idx * previewCardWidth;
				float y = screenSize.Y / 2f - 150f;
				if (size > 5)
					y = screenSize.Y / 2f - 50f - previewCardWidth * line;

				// 向左偏移，出现在玩家头顶附近
				targetPos = targetParent.ToLocal(new Vector2(screenSize.X / 2f + x - 250f, y - 100f));
				targetRot = 0f;
				targetModulate = Colors.White;
			}
			else
			{
				// 计算位置和变换 —— 完全复刻原来的完美环绕坐标
				float xP = 0f;
				float xC = xP + (0.5f - 2 * Mathf.Abs(rRate - 0.5f)) * width;

				float k = 0.4f * width;
				float yOffset = 0.1f * width - k * Mathf.Pow(0.5f - 2 * Mathf.Abs(rRate - 0.5f), 2);
				if (rRate >= 0.5f)
					yOffset = -yOffset;

				float yC = yOffset + YOffset;

				float xScale = Mathf.Sqrt(Mathf.Pow(width / 2f, 2) - Mathf.Pow(xC - xP, 2)) / (width / 2f);
				if (xScale < 0) xScale = 0;

				float rotationDeg = (0.5f - 2 * Mathf.Abs(rRate - 0.5f)) * 60;

				// NCard 使用固定比例缩放，大小与原来 Sprite2D 相近，再放大1.6倍
				float scaleX = CircleDrawScale * xScale * 1.6f;
				float scaleY = CircleDrawScale * 1.6f;

				targetPos = new Vector2(xC, yC);
				targetScale = new Vector2(scaleX, scaleY);
				targetRot = rotationDeg;
				targetModulate = isBehind ? new Color(BackDarken, BackDarken, BackDarken, 1f) : Colors.White;
			}

			if (delta > 0)
			{
				_currentPositions[i] = _currentPositions[i].Lerp(targetPos, LerpSpeed * delta);
				_currentScales[i] = _currentScales[i].Lerp(targetScale, LerpSpeed * delta);
				_currentRotations[i] = Mathf.Lerp(_currentRotations[i], targetRot, LerpSpeed * delta);
			}
			else
			{
				_currentPositions[i] = targetPos;
				_currentScales[i] = targetScale;
				_currentRotations[i] = targetRot;
			}

			card.Position = _currentPositions[i];
			card.Scale = _currentScales[i];
			card.RotationDegrees = _currentRotations[i];
			card.SelfModulate = targetModulate;
			card.Visible = true;
		}
	}
}
