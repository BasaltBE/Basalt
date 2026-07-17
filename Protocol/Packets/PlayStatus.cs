using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.PlayStatus)]
public sealed record PlayStatusPacket : DataPacket
{
    /// <summary>
    /// Status of the session.
    /// Whether the client is able to log in or not, and if it is able to spawn in
    /// </summary>
    public PlayStatus Status;

    public PlayStatusPacket()
    {
    }

    public PlayStatusPacket(PlayStatus status)
    {
        Status = status;
    }

    public override void Deserialize(BinaryReader reader)
    {
        Status = (PlayStatus)reader.ReadInt32(false);
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32((int)Status, false);
    }
}
