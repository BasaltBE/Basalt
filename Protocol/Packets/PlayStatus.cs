using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record PlayStatusPacket : DataPacket
{
    public PlayStatus Status { get; set; }

    public PlayStatusPacket() {}

    public PlayStatusPacket(PlayStatus status)
    {
        Status = status;
    }

    public override PacketId PacketId => PacketId.PlayStatus;

    public override void Deserialize(ref BinaryReader reader)
    {
        Status = (PlayStatus)reader.ReadInt32(false);
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteInt32((int)Status, false);
    }
}
