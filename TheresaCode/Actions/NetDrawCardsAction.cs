using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace Theresa.TheresaCode.Actions;

/// <summary>
/// NetDrawCardsAction：DrawCardsAction 的网络序列化版本
/// </summary>
public struct NetDrawCardsAction : INetAction
{
    /// <summary>
    /// 要抽取的卡牌数量
    /// </summary>
    public uint Count;

    /// <summary>
    /// 将网络动作转换回本地可执行的 GameAction
    /// </summary>
    public GameAction ToGameAction(Player player)
    {
        return new DrawCardsAction(player, Count, true);
    }

    /// <summary>
    /// 将数据写入网络封包（发送时调用）
    /// </summary>
    public void Serialize(PacketWriter writer)
    {
        writer.WriteUInt(Count, 32); // 使用32位存储uint
    }

    /// <summary>
    /// 从网络封包读取数据（接收时调用）
    /// </summary>
    public void Deserialize(PacketReader reader)
    {
        Count = reader.ReadUInt(32);
    }
}