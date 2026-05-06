using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record SetLocalPlayerAsInitializedPacket : DataPacket
{
    public ulong EntityRuntimeId { get; set; }

    public override PacketId PacketId => PacketId.SetLocalPlayerAsInitialized;

    public override void Deserialize(ref BinaryReader reader)
    {
        EntityRuntimeId = reader.ReadVarULong();
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteVarULong(EntityRuntimeId);
    }
}
