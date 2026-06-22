using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using Theresa.TheresaCode.Dust;

namespace Theresa.TheresaCode.Actions;

/// <summary>
/// DustItAction：将一张微尘牌随机打出（萦绕效果）。
/// 通过 GameAction 机制在联机两端同步执行，确保 host/client 选择并打出同一张卡牌。
/// </summary>
public sealed class DustItAction : GameAction
{
    private readonly Player _player;
    private readonly bool _toTop;
    private readonly bool _exhaustIt;
    private readonly NetCombatCard? _selectedCard;
    private readonly uint? _targetCombatId;

    public override ulong OwnerId => _player.NetId;
    public override GameActionType ActionType => GameActionType.Combat;

    /// <summary>
    /// 本地创建 action：由触发端（通常是主机或该 action 的 owner）随机选择一张 dust 卡牌并序列化。
    /// </summary>
    public DustItAction(Player player, bool toTop = false, bool exhaustIt = false, Creature? target = null)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _toTop = toTop;
        _exhaustIt = exhaustIt;

        // 仅在该 action 的 owner 端执行随机选择，随后通过 NetDustItAction 同步给其他玩家。
        // 使用 NetService.NetId == OwnerId 判断，而不是 LocalContext.IsMe(player)，
        // 因为主机可能代理执行非本地玩家的 action。
        if (RunManager.Instance?.NetService?.NetId == _player.NetId)
        {
            var dustCards = DustManager.CardsFor(player)
                .Where(c => c.Owner == player && !DustManager.IsCurrentlyLingering(c))
                .ToList();

            CardModel? selectedCard = null;
            if (dustCards.Count > 0)
            {
                var rng = player.RunState.Rng.Shuffle;
                for (int i = dustCards.Count - 1; i > 0; i--)
                {
                    int j = rng.NextInt(i + 1);
                    (dustCards[i], dustCards[j]) = (dustCards[j], dustCards[i]);
                }
                selectedCard = dustCards[0];
            }

            Creature? selectedTarget = target;
            if (selectedCard != null)
            {
                var combatState = player.Creature?.CombatState;
                if (combatState != null)
                {
                    if (selectedCard.TargetType == TargetType.AnyEnemy)
                        selectedTarget = player.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies);
                    else if (selectedCard.TargetType == TargetType.AnyAlly)
                        selectedTarget = player.RunState.Rng.CombatTargets.NextItem(combatState.Allies.Where(c => c != null && c.IsAlive));
                    else if (selectedCard.TargetType == TargetType.Self)
                        selectedTarget = player.Creature;
                }

                _selectedCard = NetCombatCard.FromModel(selectedCard);
            }

            _targetCombatId = selectedTarget?.CombatId;
        }
        else
        {
            _targetCombatId = target?.CombatId;
        }
    }

    /// <summary>
    /// 从网络反序列化创建 action：使用发送端已经选定的 NetCombatCard。
    /// </summary>
    public DustItAction(Player player, bool toTop, bool exhaustIt, NetCombatCard? selectedCard, uint? targetCombatId, bool fromNet)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _toTop = toTop;
        _exhaustIt = exhaustIt;
        _selectedCard = selectedCard;
        _targetCombatId = targetCombatId;
    }

    protected override async Task ExecuteAction()
    {
        if (_player == null) return;

        if (_selectedCard == null)
        {
            MainFile.Logger?.Info("[DustItAction] No card selected, skipping");
            return;
        }

        var card = _selectedCard.Value.ToCardModelOrNull();
        if (card == null)
        {
            MainFile.Logger?.Info($"[DustItAction] Selected card index {_selectedCard.Value.CombatCardIndex} not found, skipping");
            return;
        }

        var choiceContext = new GameActionPlayerChoiceContext(this);
        await DustManager.ExecuteLingeredCard(_player, card, _toTop, _exhaustIt, _targetCombatId, choiceContext);
    }

    public override INetAction ToNetAction()
    {
        return new NetDustItAction
        {
            ToTop = _toTop,
            ExhaustIt = _exhaustIt,
            SelectedCard = _selectedCard,
            TargetCombatId = _targetCombatId
        };
    }
}

public struct NetDustItAction : INetAction
{
    public bool ToTop;
    public bool ExhaustIt;
    public NetCombatCard? SelectedCard;
    public uint? TargetCombatId;

    public GameAction ToGameAction(Player player)
    {
        return new DustItAction(player, ToTop, ExhaustIt, SelectedCard, TargetCombatId, fromNet: true);
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteBool(ToTop);
        writer.WriteBool(ExhaustIt);
        writer.WriteBool(SelectedCard.HasValue);
        if (SelectedCard.HasValue)
        {
            writer.Write(SelectedCard.Value);
        }
        writer.WriteBool(TargetCombatId.HasValue);
        if (TargetCombatId.HasValue)
        {
            writer.WriteUInt(TargetCombatId.Value, 32);
        }
    }

    public void Deserialize(PacketReader reader)
    {
        ToTop = reader.ReadBool();
        ExhaustIt = reader.ReadBool();
        if (reader.ReadBool())
        {
            SelectedCard = reader.Read<NetCombatCard>();
        }
        else
        {
            SelectedCard = null;
        }
        if (reader.ReadBool())
        {
            TargetCombatId = reader.ReadUInt(32);
        }
        else
        {
            TargetCombatId = null;
        }
    }
}
