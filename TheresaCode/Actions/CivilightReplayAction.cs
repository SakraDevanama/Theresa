using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Utils;

namespace Theresa.TheresaCode.Actions;

/// <summary>
/// CivilightReplayAction：文明的存续重现效果的 GameAction
/// 
/// 联机同步说明：
/// 重现机制涉及 UI 选择和卡牌创建，直接放在 OnPlay 中会导致两端独立执行而分歧。
/// 解决方案：
/// 1. 本地玩家做 UI 选择，确定要复制的卡牌参数
/// 2. 通过 CivilightReplayAction 将选择结果同步到所有客户端
/// 3. 所有客户端根据同步参数创建完全相同的卡牌副本
/// </summary>
public sealed class CivilightReplayAction : GameAction
{
    private readonly Player _player;
    private readonly string _cardId;
    private readonly int _upgradeLevel;
    private readonly string? _enchantmentId;
    private readonly int _enchantmentAmount;
    private readonly PileType _targetPile;
    private readonly bool _exhaustCopy;
    private readonly bool _etherealCopy;
    private readonly int _costDiff;

    public override ulong OwnerId => _player.NetId;
    public override GameActionType ActionType => GameActionType.Combat;

    /// <summary>
    /// 构造函数：用于本地创建动作
    /// </summary>
    public CivilightReplayAction(
        Player player,
        string cardId,
        int upgradeLevel,
        string? enchantmentId,
        int enchantmentAmount,
        PileType targetPile,
        bool exhaustCopy,
        bool etherealCopy,
        int costDiff)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _cardId = cardId ?? throw new ArgumentNullException(nameof(cardId));
        _upgradeLevel = upgradeLevel;
        _enchantmentId = enchantmentId;
        _enchantmentAmount = enchantmentAmount;
        _targetPile = targetPile;
        _exhaustCopy = exhaustCopy;
        _etherealCopy = etherealCopy;
        _costDiff = costDiff;
    }

    /// <summary>
    /// 构造函数：用于从网络反序列化时重建动作
    /// </summary>
    public CivilightReplayAction(
        Player player,
        string cardId,
        int upgradeLevel,
        string? enchantmentId,
        int enchantmentAmount,
        PileType targetPile,
        bool exhaustCopy,
        bool etherealCopy,
        int costDiff,
        bool fromNet)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _cardId = cardId ?? throw new ArgumentNullException(nameof(cardId));
        _upgradeLevel = upgradeLevel;
        _enchantmentId = enchantmentId;
        _enchantmentAmount = enchantmentAmount;
        _targetPile = targetPile;
        _exhaustCopy = exhaustCopy;
        _etherealCopy = etherealCopy;
        _costDiff = costDiff;
    }

    protected override async Task ExecuteAction()
    {
        var combatState = _player.Creature?.CombatState;
        if (combatState == null)
        {
            Theresa.MainFile.Logger?.Warn($"[CivilightReplayAction] CombatState is null for player {_player.NetId}");
            Cancel();
            return;
        }

        // 从 ModelDb 获取卡牌原型
        var modelId = ModelId.Deserialize(_cardId);
        if (modelId == null)
        {
            Theresa.MainFile.Logger?.Warn($"[CivilightReplayAction] Failed to deserialize card ID: {_cardId}");
            Cancel();
            return;
        }

        var canonicalCard = ModelDb.GetById<CardModel>(modelId);
        if (canonicalCard == null)
        {
            Theresa.MainFile.Logger?.Warn($"[CivilightReplayAction] Card not found in ModelDb: {_cardId}");
            Cancel();
            return;
        }

        // 创建卡牌副本
        var copiedCard = combatState.CreateCard(canonicalCard, _player)
            ?? _player.RunState.CreateCard(canonicalCard, _player);

        // 应用升级等级
        while (copiedCard.CurrentUpgradeLevel < _upgradeLevel)
        {
            copiedCard.UpgradeInternal();
        }
        copiedCard.FinalizeUpgradeInternal();

        // 应用附魔
        if (!string.IsNullOrEmpty(_enchantmentId))
        {
            var enchantmentModelId = ModelId.Deserialize(_enchantmentId);
            if (enchantmentModelId != null)
            {
                var enchantmentCanonical = ModelDb.GetById<EnchantmentModel>(enchantmentModelId);
                if (enchantmentCanonical != null)
                {
                    var enchantment = enchantmentCanonical.ToMutable();
                    CardCmd.Enchant(enchantment, copiedCard, _enchantmentAmount);
                }
            }
        }

        // 添加虚无
        if (_etherealCopy)
            copiedCard.AddKeyword(CardKeyword.Ethereal);

        // 添加消耗
        if (_exhaustCopy)
            copiedCard.AddKeyword(CardKeyword.Exhaust);

        // 联机模式下：添加 Dim（黯淡）关键词，防止重现牌进入微尘牌堆
        if (RunManager.Instance?.NetService?.Type.IsMultiplayer() == true)
        {
            copiedCard.AddKeyword(DimKeyword.Dim);
        }

        // 费用调整
        if (copiedCard.EnergyCost.GetResolved() > 0 && _costDiff != 0)
        {
            var newCost = copiedCard.EnergyCost.GetResolved() + _costDiff;
            if (newCost < 0) newCost = 0;
            copiedCard.EnergyCost.AddThisTurnOrUntilPlayed(newCost - copiedCard.EnergyCost.GetResolved());
        }

        // 将复制牌放入目标牌堆
        Theresa.MainFile.Logger?.Info($"[CivilightReplayAction] Adding {_cardId} to {_targetPile} for player {_player.NetId}");
        
        switch (_targetPile)
        {
            case PileType.Draw:
                await CardPileCmd.Add(copiedCard, PileType.Draw, CardPilePosition.Top, null);
                break;
            case PileType.Hand:
                await CardPileCmd.Add(copiedCard, PileType.Hand, CardPilePosition.Top, null);
                break;
            case PileType.Discard:
                await CardPileCmd.Add(copiedCard, PileType.Discard, CardPilePosition.Top, null);
                break;
            case PileType.Exhaust:
                await CardPileCmd.Add(copiedCard, PileType.Exhaust, CardPilePosition.Top, null);
                break;
            default:
                await CardPileCmd.Add(copiedCard, PileType.Hand, CardPilePosition.Top, null);
                break;
        }
    }

    public override INetAction ToNetAction()
    {
        return new NetCivilightReplayAction
        {
            CardId = _cardId,
            UpgradeLevel = _upgradeLevel,
            EnchantmentId = _enchantmentId,
            EnchantmentAmount = _enchantmentAmount,
            TargetPile = (int)_targetPile,
            ExhaustCopy = _exhaustCopy,
            EtherealCopy = _etherealCopy,
            CostDiff = _costDiff
        };
    }
}

public struct NetCivilightReplayAction : INetAction
{
    public string CardId;
    public int UpgradeLevel;
    public string? EnchantmentId;
    public int EnchantmentAmount;
    public int TargetPile;
    public bool ExhaustCopy;
    public bool EtherealCopy;
    public int CostDiff;

    public GameAction ToGameAction(Player player)
    {
        return new CivilightReplayAction(
            player,
            CardId,
            UpgradeLevel,
            EnchantmentId,
            EnchantmentAmount,
            (PileType)TargetPile,
            ExhaustCopy,
            EtherealCopy,
            CostDiff,
            fromNet: true
        );
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteString(CardId);
        writer.WriteInt(UpgradeLevel);
        writer.WriteString(EnchantmentId ?? "");
        writer.WriteInt(EnchantmentAmount);
        writer.WriteInt(TargetPile);
        writer.WriteBool(ExhaustCopy);
        writer.WriteBool(EtherealCopy);
        writer.WriteInt(CostDiff);
    }

    public void Deserialize(PacketReader reader)
    {
        CardId = reader.ReadString();
        UpgradeLevel = reader.ReadInt();
        var encId = reader.ReadString();
        EnchantmentId = string.IsNullOrEmpty(encId) ? null : encId;
        EnchantmentAmount = reader.ReadInt();
        TargetPile = reader.ReadInt();
        ExhaustCopy = reader.ReadBool();
        EtherealCopy = reader.ReadBool();
        CostDiff = reader.ReadInt();
    }
}
