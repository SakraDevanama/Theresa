using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace Theresa.TheresaCode.Actions;

public struct NetConvertToDustAction : INetAction
{
    public NetCombatCard Card;

    public GameAction ToGameAction(Player player)
    {
        return new ConvertToDustAction(player, Card);
    }

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Card);
    }

    public void Deserialize(PacketReader reader)
    {
        Card = reader.Read<NetCombatCard>();
    }
}
