using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.SetPlayerGameType)]
public sealed record SetPlayerGameTypePacket : DataPacket
{
    /// <summary>
    /// Game type to apply to the local player.
    /// </summary>
    public Gamemode GameType;

    public override void Deserialize(BinaryReader reader)
    {
        GameType = (Gamemode)reader.ReadVarInt();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarInt((int)GameType);
    }
}
