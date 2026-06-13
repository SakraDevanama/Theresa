using System.Linq;
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
using Theresa.TheresaCode.Dust;

namespace Theresa.TheresaCode.Actions;

/// <summary>
/// DustItAction：将一张微尘牌随机打出（萦绕效果）
/// </summary>
public sealed class DustItAction : GameAction
{
    private readonly Player _player;
    private readonly bool _toTop;
    private readonly bool _exhaustIt;
    private readonly string? _selectedCardId;
    private readonly uint? _targetCombatId;

    public override ulong OwnerId => _player.NetId;
    public override GameActionType ActionType => GameActionType.Combat;

    public DustItAction(Player player, bool toTop = false, bool exhaustIt = false, Creature? target = null)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _toTop = toTop;
        _exhaustIt = exhaustIt;

        if (LocalContext.IsMe(player))
        {
            var dustCards = DustManager.Cards.Where(c => c.Owner == player).ToList();
            CardModel? selectedCard = null;
            if (dustCards.Count > 0)
            {
                // 使用 RNG 随机选择（与 DustManager.DustIt 一致）
                var rng = player.RunState.Rng.Shuffle;
                for (int i = dustCards.Count - 1; i > 0; i--)
                {
                    int j = rng.NextInt(i + 1);
                    (dustCards[i], dustCards[j]) = (dustCards[j], dustCards[i]);
                }
                selectedCard = dustCards[0];
            }
            Creature? selectedTarget = null;
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
            }
            _selectedCardId = selectedCard?.Id.Entry;
            _targetCombatId = selectedTarget?.CombatId ?? target?.CombatId;
        }
        else
        {
            _selectedCardId = null;
            _targetCombatId = target?.CombatId;
        }
    }

    public DustItAction(Player player, bool toTop, bool exhaustIt, string? selectedCardId, uint? targetCombatId, bool fromNet)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _toTop = toTop;
        _exhaustIt = exhaustIt;
        _selectedCardId = selectedCardId;
        _targetCombatId = targetCombatId;
    }

    protected override async Task ExecuteAction()
    {
        if (_player == null) return;

        var choiceContext = new GameActionPlayerChoiceContext(this);
        await DustItWithSelection(_player, _toTop, _exhaustIt, _selectedCardId, _targetCombatId, choiceContext);
    }

    public override INetAction ToNetAction()
    {
        return new NetDustItAction
        {
            ToTop = _toTop,
            ExhaustIt = _exhaustIt,
            SelectedCardId = _selectedCardId,
            TargetCombatId = _targetCombatId
        };
    }

    private static async Task DustItWithSelection(Player player, bool toTop, bool exhaustIt, string? selectedCardId, uint? targetCombatId, PlayerChoiceContext choiceContext)
    {
        if (string.IsNullOrEmpty(selectedCardId)) return;
        
        var card = DustManager.Cards.FirstOrDefault(c => c.Id.Entry == selectedCardId && c.Owner == player);
        if (card == null) return;
        
        await DustManager.RemoveCard(card);
        
        var copy = card.CreateClone();
        var combatState = player.Creature?.CombatState;
        Creature? target = null;
        if (targetCombatId.HasValue && combatState != null)
            target = await combatState.GetCreatureAsync(targetCombatId.Value, 10.0);
        
        if (combatState != null)
        {
            await CardCmd.AutoPlay(choiceContext, copy, target);
            await CardPileCmd.RemoveFromCombat(copy);
        }
        
        if (exhaustIt)
            await CardPileCmd.Add(card, PileType.Exhaust);
        else if (toTop)
            await CardPileCmd.Add(card, PileType.Draw);
    }
}

public struct NetDustItAction : INetAction
{
    public bool ToTop;
    public bool ExhaustIt;
    public string? SelectedCardId;
    public uint? TargetCombatId;

    public GameAction ToGameAction(Player player)
    {
        return new DustItAction(player, ToTop, ExhaustIt, SelectedCardId, TargetCombatId, fromNet: true);
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteBool(ToTop);
        writer.WriteBool(ExhaustIt);
        writer.WriteBool(SelectedCardId != null);
        if (SelectedCardId != null)
        {
            writer.WriteString(SelectedCardId);
        }
        writer.WriteBool(TargetCombatId.HasValue);
        if (TargetCombatId.HasValue)
        {
            writer.WriteUInt(TargetCombatId.Value);
        }
    }

    public void Deserialize(PacketReader reader)
    {
        ToTop = reader.ReadBool();
        ExhaustIt = reader.ReadBool();
        if (reader.ReadBool())
        {
            SelectedCardId = reader.ReadString();
        }
        else
        {
            SelectedCardId = null;
        }
        if (reader.ReadBool())
        {
            TargetCombatId = reader.ReadUInt();
        }
        else
        {
            TargetCombatId = null;
        }
    }
}
