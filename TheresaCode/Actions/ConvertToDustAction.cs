using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Dust;

namespace Theresa.TheresaCode.Actions;

public sealed class ConvertToDustAction : GameAction
{
    private readonly Player _player;
    private readonly NetCombatCard _netCard;
    private CardModel? _card;

    public override ulong OwnerId => _player.NetId;
    public override GameActionType ActionType => GameActionType.Combat;

    public ConvertToDustAction(Player player, CardModel card)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _card = card ?? throw new ArgumentNullException(nameof(card));
        _netCard = NetCombatCard.FromModel(card);
    }

    public ConvertToDustAction(Player player, NetCombatCard netCard)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _netCard = netCard;
    }

    protected override async Task ExecuteAction()
    {
        _card ??= _netCard.ToCardModel();
        if (_card == null)
        {
            Cancel();
            return;
        }

        if (DustManager.ContainsCard(_card))
        {
            Cancel();
            return;
        }

        await DustManager.AddCard(_card);
    }

    public override INetAction ToNetAction()
    {
        return new NetConvertToDustAction { Card = _netCard };
    }
}
