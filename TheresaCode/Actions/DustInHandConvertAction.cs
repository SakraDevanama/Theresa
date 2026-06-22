using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Theresa.TheresaCode.Dust;

namespace Theresa.TheresaCode.Actions;

/// <summary>
/// DustInHandConvertAction：将选中的手牌转化为微尘的 GameAction
/// 用于 DustInHand 卡牌，避免在 OnPlay 中直接调用 DustManager.AddCardOverLimit 导致 SignalAwaiter 嵌套
/// 
/// 联机同步说明：
/// 不使用 NetCombatCard（因为 NetCombatCardDb 的 ID 映射在 Host/Client 之间可能因克隆卡创建顺序不一致而错位）。
/// 改为使用手牌索引 + 卡牌类型验证来定位卡牌。
/// 因为 DrawCardsAction 是同步的，执行后两端手牌内容和顺序一致，通过索引可找到相同的卡牌。
/// </summary>
public sealed class DustInHandConvertAction : GameAction
{
    private readonly Player _player;
    private readonly int _handIndex;
    private readonly string _expectedCardId;

    public override ulong OwnerId => _player.NetId;
    public override GameActionType ActionType => GameActionType.Combat;

    /// <summary>
    /// 构造函数：用于本地创建动作
    /// </summary>
    /// <param name="player">玩家</param>
    /// <param name="handIndex">选择卡牌在手牌中的索引（0-based）</param>
    /// <param name="expectedCardId">预期卡牌的 ID（用于验证）</param>
    public DustInHandConvertAction(Player player, int handIndex, string expectedCardId)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _handIndex = handIndex;
        _expectedCardId = expectedCardId ?? throw new ArgumentNullException(nameof(expectedCardId));
    }

    /// <summary>
    /// 构造函数：用于从网络反序列化时重建动作
    /// </summary>
    public DustInHandConvertAction(Player player, int handIndex, string expectedCardId, bool fromNet)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _handIndex = handIndex;
        _expectedCardId = expectedCardId ?? throw new ArgumentNullException(nameof(expectedCardId));
    }

    protected override async Task ExecuteAction()
    {
        var hand = PileType.Hand.GetPile(_player);
        if (hand == null)
        {
            Theresa.MainFile.Logger?.Warn($"[DustInHandConvertAction] Hand pile is null for player {_player.NetId}");
            Cancel();
            return;
        }

        var handCards = hand.Cards.ToList();
        
        // 验证索引范围
        if (_handIndex < 0 || _handIndex >= handCards.Count)
        {
            Theresa.MainFile.Logger?.Warn($"[DustInHandConvertAction] Hand index {_handIndex} out of range (count={handCards.Count}) for player {_player.NetId}");
            Cancel();
            return;
        }

        var card = handCards[_handIndex];
        
        // 验证卡牌类型是否匹配（防止因手牌顺序分歧导致选错牌）
        if (card.Id.Entry != _expectedCardId)
        {
            Theresa.MainFile.Logger?.Warn($"[DustInHandConvertAction] Card mismatch at index {_handIndex}: expected {_expectedCardId}, got {card.Id.Entry}. Trying to find by ID...");
            
            // 后备方案：在手牌中搜索匹配的卡牌 ID
            card = handCards.FirstOrDefault(c => c.Id.Entry == _expectedCardId);
            if (card == null)
            {
                Theresa.MainFile.Logger?.Error($"[DustInHandConvertAction] Could not find card {_expectedCardId} in hand for player {_player.NetId}");
                Cancel();
                return;
            }
            
            Theresa.MainFile.Logger?.Info($"[DustInHandConvertAction] Found card {_expectedCardId} at fallback position");
        }

        if (DustManager.ContainsCard(card))
        {
            Theresa.MainFile.Logger?.Info($"[DustInHandConvertAction] Card {card.Id.Entry} is already dust, cancelling");
            Cancel();
            return;
        }

        // Dim 牌不应被转化为微尘
        if (card.Keywords.Contains(Theresa.TheresaCode.Keywords.DimKeyword.Dim))
        {
            Theresa.MainFile.Logger?.Info($"[DustInHandConvertAction] Card {card.Id.Entry} is Dim, cancelling");
            Cancel();
            return;
        }

        // 检查牌是否还在手牌中（双重验证）
        if (!hand.Cards.Contains(card))
        {
            Theresa.MainFile.Logger?.Warn($"[DustInHandConvertAction] Card {card.Id.Entry} is no longer in hand");
            Cancel();
            return;
        }

        // 关键：在 RemoveFromCurrentPile 之前，先从手牌UI中移除 NCard
        // 否则 skipVisuals: true 会导致 NCard 残留在手牌UI中，形成"幽灵卡牌"
        var handNode = NCombatRoom.Instance?.Ui.Hand;
        if (handNode != null)
        {
            var nCard = NCard.FindOnTable(card);
            Theresa.MainFile.Logger?.Info($"[DustInHandConvertAction] nCard found={nCard != null}, inHand={nCard != null && handNode.IsAncestorOf(nCard)}");
            if (nCard != null && handNode.IsAncestorOf(nCard))
            {
                try
                {
                    handNode.Remove(card);
                    Theresa.MainFile.Logger?.Info($"[DustInHandConvertAction] Removed {card.Id.Entry} from hand UI");
                }
                catch (Exception ex)
                {
                    Theresa.MainFile.Logger?.Error($"[DustInHandConvertAction] Failed to remove {card.Id.Entry} from hand UI: {ex.Message}");
                }
            }
        }

        card.RemoveFromCurrentPile();
        await DustManager.AddCardOverLimit(card);
    }

    public override INetAction ToNetAction()
    {
        return new NetDustInHandConvertAction 
        { 
            HandIndex = _handIndex,
            ExpectedCardId = _expectedCardId
        };
    }
}

public struct NetDustInHandConvertAction : INetAction
{
    public int HandIndex;
    public string ExpectedCardId;

    public GameAction ToGameAction(Player player)
    {
        return new DustInHandConvertAction(player, HandIndex, ExpectedCardId, fromNet: true);
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(HandIndex);
        writer.WriteString(ExpectedCardId);
    }

    public void Deserialize(PacketReader reader)
    {
        HandIndex = reader.ReadInt();
        ExpectedCardId = reader.ReadString();
    }
}
