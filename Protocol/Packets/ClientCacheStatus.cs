using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record ClientCacheStatusPacket : DataPacket
{
    public bool Enabled { get; set; }

    public override PacketId PacketId => PacketId.ClientCacheStatus;

    public override void Deserialize(ref BinaryReader reader)
    {
        Enabled = reader.ReadBool();
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteBool(Enabled);
    }
}
