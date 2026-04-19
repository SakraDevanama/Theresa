using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using Theresa.TheresaCode.Enchantments;

namespace Theresa.TheresaCode.Actions;

/// <summary>
/// EnchantCardAction：给指定卡牌附魔的同步 GameAction
/// 
/// 联机同步说明：
/// CardCmd.Enchant 是同步命令，不经过 ActionQueueSynchronizer，直接在 OnPlay 中调用会导致联机状态分歧。
/// 此 Action 将附魔操作包装为 GameAction，通过 ActionQueueSynchronizer 同步到所有客户端。
/// 
/// 同步策略：
/// 1. 使用抽牌堆索引 + 卡牌ID 来定位目标卡牌（参考 DustInHandConvertAction）
/// 2. 因为 CardSelectCmd.FromSimpleGrid 已经通过 PlayerChoiceSynchronizer 同步了选择索引
/// 3. 两端的选择列表一致，所以索引对应的卡牌也一致
/// 4. 附魔操作在 GameAction 中执行，确保两端同时执行
/// </summary>
public sealed class EnchantCardAction : GameAction
{
    private readonly Player _player;
    private readonly int _drawPileIndex;
    private readonly string _expectedCardId;
    private readonly string _enchantmentId;
    private readonly int _enchantmentAmount;

    public override ulong OwnerId => _player.NetId;
    public override GameActionType ActionType => GameActionType.Combat;

    /// <summary>
    /// 构造函数：用于本地创建动作
    /// </summary>
    /// <param name="player">玩家</param>
    /// <param name="drawPileIndex">目标卡牌在抽牌堆中的索引（0-based）</param>
    /// <param name="expectedCardId">预期卡牌的 ID（用于验证）</param>
    /// <param name="enchantmentId">附魔类型 ID</param>
    /// <param name="enchantmentAmount">附魔数值</param>
    public EnchantCardAction(Player player, int drawPileIndex, string expectedCardId, string enchantmentId, int enchantmentAmount)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _drawPileIndex = drawPileIndex;
        _expectedCardId = expectedCardId ?? throw new ArgumentNullException(nameof(expectedCardId));
        _enchantmentId = enchantmentId ?? throw new ArgumentNullException(nameof(enchantmentId));
        _enchantmentAmount = enchantmentAmount;
    }

    /// <summary>
    /// 构造函数：用于从网络反序列化时重建动作
    /// </summary>
    public EnchantCardAction(Player player, int drawPileIndex, string expectedCardId, string enchantmentId, int enchantmentAmount, bool fromNet)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _drawPileIndex = drawPileIndex;
        _expectedCardId = expectedCardId ?? throw new ArgumentNullException(nameof(expectedCardId));
        _enchantmentId = enchantmentId ?? throw new ArgumentNullException(nameof(enchantmentId));
        _enchantmentAmount = enchantmentAmount;
    }

    protected override async Task ExecuteAction()
    {
        MainFile.Logger?.Info($"[EnchantCardAction] ExecuteAction START: player={_player.NetId}, index={_drawPileIndex}, cardId={_expectedCardId}, enchantmentId={_enchantmentId}");
        
        var drawPile = PileType.Draw.GetPile(_player);
        if (drawPile == null)
        {
            MainFile.Logger?.Warn($"[EnchantCardAction] Draw pile is null for player {_player.NetId}");
            Cancel();
            return;
        }

        var drawCards = drawPile.Cards.ToList();
        MainFile.Logger?.Info($"[EnchantCardAction] Draw pile has {drawCards.Count} cards: [{string.Join(", ", drawCards.Select(c => c.Id.Entry))}]");

        // 验证索引范围
        if (_drawPileIndex < 0 || _drawPileIndex >= drawCards.Count)
        {
            MainFile.Logger?.Warn($"[EnchantCardAction] Draw pile index {_drawPileIndex} out of range (count={drawCards.Count}) for player {_player.NetId}");
            Cancel();
            return;
        }

        var card = drawCards[_drawPileIndex];
        MainFile.Logger?.Info($"[EnchantCardAction] Card at index {_drawPileIndex}: {card.Id.Entry}");

        // 验证卡牌ID是否匹配
        if (card.Id.Entry != _expectedCardId)
        {
            MainFile.Logger?.Warn($"[EnchantCardAction] Card mismatch at index {_drawPileIndex}: expected {_expectedCardId}, got {card.Id.Entry}. Trying to find by ID...");

            // 后备方案：在抽牌堆中搜索匹配的卡牌ID
            card = drawCards.FirstOrDefault(c => c.Id.Entry == _expectedCardId);
            if (card == null)
            {
                MainFile.Logger?.Error($"[EnchantCardAction] Could not find card {_expectedCardId} in draw pile for player {_player.NetId}");
                Cancel();
                return;
            }

            MainFile.Logger?.Info($"[EnchantCardAction] Found card {_expectedCardId} at fallback position");
        }

        // 验证卡牌是否还在抽牌堆中
        if (!drawPile.Cards.Contains(card))
        {
            MainFile.Logger?.Warn($"[EnchantCardAction] Card {card.Id.Entry} is no longer in draw pile");
            Cancel();
            return;
        }

        // 验证卡牌是否可以被附魔
        if (card.Enchantment != null)
        {
            MainFile.Logger?.Info($"[EnchantCardAction] Card {card.Id.Entry} already has enchantment {card.Enchantment.GetType().Name}, cancelling");
            Cancel();
            return;
        }

        // 创建附魔实例
        MainFile.Logger?.Info($"[EnchantCardAction] Deserializing enchantment ModelId: {_enchantmentId}");
        var enchantmentModelId = ModelId.Deserialize(_enchantmentId);
        MainFile.Logger?.Info($"[EnchantCardAction] Deserialized ModelId: category={enchantmentModelId.Category}, entry={enchantmentModelId.Entry}");
        
        var enchantmentCanonical = ModelDb.GetById<EnchantmentModel>(enchantmentModelId);
        if (enchantmentCanonical == null)
        {
            MainFile.Logger?.Error($"[EnchantCardAction] Enchantment {_enchantmentId} not found in ModelDb");
            Cancel();
            return;
        }
        MainFile.Logger?.Info($"[EnchantCardAction] Found enchantment canonical: {enchantmentCanonical.GetType().Name}");

        var enchantment = enchantmentCanonical.ToMutable();
        if (enchantment == null)
        {
            MainFile.Logger?.Error($"[EnchantCardAction] Failed to create mutable enchantment {_enchantmentId}");
            Cancel();
            return;
        }

        // 执行附魔
        try
        {
            MainFile.Logger?.Info($"[EnchantCardAction] Calling CardCmd.Enchant on {card.Id.Entry} with {enchantment.GetType().Name} amount={_enchantmentAmount}");
            CardCmd.Enchant(enchantment, card, _enchantmentAmount);
            MainFile.Logger?.Info($"[EnchantCardAction] Successfully enchanted {card.Id.Entry} with {_enchantmentId} (amount={_enchantmentAmount}). Card now has enchantment: {card.Enchantment?.GetType().Name ?? "null"}");
        }
        catch (InvalidOperationException ex)
        {
            MainFile.Logger?.Warn($"[EnchantCardAction] Failed to enchant {card.Id.Entry}: {ex.Message}");
            Cancel();
        }
        
        MainFile.Logger?.Info($"[EnchantCardAction] ExecuteAction END");
    }

    public override INetAction ToNetAction()
    {
        return new NetEnchantCardAction
        {
            DrawPileIndex = _drawPileIndex,
            ExpectedCardId = _expectedCardId,
            EnchantmentId = _enchantmentId,
            EnchantmentAmount = _enchantmentAmount
        };
    }
}

public struct NetEnchantCardAction : INetAction
{
    public int DrawPileIndex;
    public string ExpectedCardId;
    public string EnchantmentId;
    public int EnchantmentAmount;

    public GameAction ToGameAction(Player player)
    {
        return new EnchantCardAction(player, DrawPileIndex, ExpectedCardId, EnchantmentId, EnchantmentAmount, fromNet: true);
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(DrawPileIndex);
        writer.WriteString(ExpectedCardId);
        writer.WriteString(EnchantmentId);
        writer.WriteInt(EnchantmentAmount);
    }

    public void Deserialize(PacketReader reader)
    {
        DrawPileIndex = reader.ReadInt();
        ExpectedCardId = reader.ReadString();
        EnchantmentId = reader.ReadString();
        EnchantmentAmount = reader.ReadInt();
    }
}
