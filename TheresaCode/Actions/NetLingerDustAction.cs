using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace Theresa.TheresaCode.Actions;

public struct NetLingerDustAction : INetAction
{
    public NetCombatCard Card;
    public bool ToTop;
    public bool ExhaustIt;
    public uint? TargetCombatId;

    public GameAction ToGameAction(Player player)
    {
        return new LingerDustAction(player, Card, ToTop, ExhaustIt, TargetCombatId);
    }

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Card);
        writer.WriteBool(ToTop);
        writer.WriteBool(ExhaustIt);
        writer.WriteBool(TargetCombatId.HasValue);
        if (TargetCombatId.HasValue)
            writer.WriteUInt(TargetCombatId.Value, 6);
    }

    public void Deserialize(PacketReader reader)
    {
        Card = reader.Read<NetCombatCard>();
        ToTop = reader.ReadBool();
        ExhaustIt = reader.ReadBool();
        TargetCombatId = reader.ReadBool() ? reader.ReadUInt(6) : null;
    }
}
