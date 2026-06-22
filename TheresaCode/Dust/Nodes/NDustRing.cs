using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Theresa.TheresaCode.Dust.Nodes;

public partial class NDustRing : Node2D
{
	private const float CircleDrawScale = 0.10f;
	private const float PreviewDrawScale = 0.7f;
	private const float YOffset = -90f;
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
	private Player? _ownerPlayer;

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _ExitTree()
	{
		if (Instance == this)
			Instance = null;
	}

	public void Initialize(NCreature parentCreature)
	{
		_parentCreature = parentCreature;
		_ownerPlayer = parentCreature.Entity?.Player;

		_backContainer = new Node2D();
		_backContainer.Name = "DustBackContainer";
		parentCreature.AddChild(_backContainer);
		var visualsNode = (Node?)parentCreature.Visuals;
		int visualsIndex = visualsNode?.GetIndex() ?? 0;
		parentCreature.MoveChild(_backContainer, visualsIndex);

		_frontContainer = new Node2D();
		_frontContainer.Name = "DustFrontContainer";
		parentCreature.AddChild(_frontContainer);
		parentCreature.MoveChild(_frontContainer, -1);

		var previewLayer = new CanvasLayer();
		previewLayer.Name = "DustPreviewLayer";
		previewLayer.Layer = 10;
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

	private IReadOnlyList<CardModel> GetDustCards()
	{
		if (_ownerPlayer != null)
			return DustManager.CardsFor(_ownerPlayer);
		return DustManager.Cards;
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
		int index = GetDustCards().ToList().IndexOf(card);
		if (index < 0 || index >= _cards.Count) return null;
		return _cards[index].GlobalPosition;
	}

	public void Refresh()
	{
		var dustCards = GetDustCards();
		int targetCount = dustCards.Count;

		if (_backContainer != null)
		{
			foreach (var child in _backContainer.GetChildren())
			{
				if (child is NCard card)
				{
					card.GetParent()?.RemoveChild(card);
					card.Model = null;
					NodePool.Free(card);
				}
			}
			while (_backContainer.GetChildCount() > 0)
				_backContainer.RemoveChild(_backContainer.GetChild(0));
		}

		if (_frontContainer != null)
		{
			foreach (var child in _frontContainer.GetChildren())
			{
				if (child is NCard card)
				{
					card.GetParent()?.RemoveChild(card);
					card.Model = null;
					NodePool.Free(card);
				}
			}
			while (_frontContainer.GetChildCount() > 0)
				_frontContainer.RemoveChild(_frontContainer.GetChild(0));
		}

		if (_previewContainer != null)
		{
			foreach (var child in _previewContainer.GetChildren())
			{
				if (child is NCard card)
				{
					card.GetParent()?.RemoveChild(card);
					card.Model = null;
					NodePool.Free(card);
				}
			}
			while (_previewContainer.GetChildCount() > 0)
				_previewContainer.RemoveChild(_previewContainer.GetChild(0));
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
		var dustCards = GetDustCards();
		int size = dustCards.Count;

		if (size != _cards.Count)
		{
			Refresh();
			return;
		}

		if (size == 0 || _parentCreature == null || _backContainer == null || _frontContainer == null) return;

		bool isPreviewMode = _parentCreature.IsFocused;

		float cardWidth = CircleDrawScale * 250f;
		float width = 150f * 1.1f + cardWidth;
		float tmpWidth = 2f * cardWidth;
		width = Math.Max(width, tmpWidth);

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
			var targetParent = isPreviewMode ? _previewContainer : (isBehind ? _backContainer : _frontContainer);
			if (card.GetParent() != targetParent)
			{
				var oldParent = card.GetParent() as Node2D;
				var newParent2D = targetParent as Node2D;
				if (oldParent != null && newParent2D != null)
				{
					_currentPositions[i] = oldParent.ToGlobal(_currentPositions[i]);
					if (targetParent != _previewContainer)
					{
						_currentPositions[i] = newParent2D.ToLocal(_currentPositions[i]);
					}
				}
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

				targetPos = new Vector2(screenSize.X / 2f + x - 250f, y - 100f);
				targetRot = 0f;
				targetModulate = Colors.White;
			}
			else
			{
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
