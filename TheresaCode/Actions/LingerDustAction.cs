using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Dust;

namespace Theresa.TheresaCode.Actions;

public sealed class LingerDustAction : GameAction
{
    private readonly Player _player;
    private readonly NetCombatCard _netCard;
    private readonly bool _toTop;
    private readonly bool _exhaustIt;
    private readonly uint? _targetCombatId;

    public override ulong OwnerId => _player.NetId;
    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;

    public LingerDustAction(Player player, CardModel card, bool toTop, bool exhaustIt, Creature? target)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _netCard = NetCombatCard.FromModel(card ?? throw new ArgumentNullException(nameof(card)));
        _toTop = toTop;
        _exhaustIt = exhaustIt;
        _targetCombatId = target?.CombatId;
    }

    public LingerDustAction(Player player, NetCombatCard netCard, bool toTop, bool exhaustIt, uint? targetCombatId)
    {
        _player = player;
        _netCard = netCard;
        _toTop = toTop;
        _exhaustIt = exhaustIt;
        _targetCombatId = targetCombatId;
    }

    protected override async Task ExecuteAction()
    {
        var card = _netCard.ToCardModel();
        if (card == null)
        {
            Cancel();
            return;
        }

        Creature? target = null;
        if (_targetCombatId.HasValue && _player.Creature?.CombatState != null)
        {
            target = await _player.Creature.CombatState.GetCreatureAsync(_targetCombatId.Value, 10.0);
        }

        // 从 Dust 中移除卡牌
        await DustManager.RemoveCard(card);
        
        // 创建副本并打出
        var copy = card.CreateClone();
        var combatState = _player.Creature?.CombatState;
        if (combatState != null)
        {
            await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(), copy, target);
            await CardPileCmd.RemoveFromCombat(copy);
        }
        
        // 处理 exhaust
        if (_exhaustIt)
        {
            await CardPileCmd.Add(card, PileType.Exhaust);
        }
        else if (_toTop)
        {
            await CardPileCmd.Add(card, PileType.Draw);
        }
    }

    public override INetAction ToNetAction()
    {
        return new NetLingerDustAction
        {
            Card = _netCard,
            ToTop = _toTop,
            ExhaustIt = _exhaustIt,
            TargetCombatId = _targetCombatId
        };
    }
}
