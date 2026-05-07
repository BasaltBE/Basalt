using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using ProtoAttribute = Basalt.Protocol.Types.Attribute;

namespace Basalt.Protocol.Packets;

public sealed record UpdateAttributesPacket : DataPacket
{
    public ulong RuntimeId { get; set; }
    public List<ProtoAttribute> Attributes { get; set; } = [];
    public ulong Tick { get; set; }

    public override PacketId PacketId => PacketId.UpdateAttributes;

    public override void Deserialize(ref BinaryReader reader)
    {
        RuntimeId = reader.ReadVarULong();
        Attributes = ProtoAttribute.ReadList(ref reader);
        Tick = reader.ReadVarULong();
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteVarULong(RuntimeId);
        ProtoAttribute.WriteList(ref writer, Attributes);
        writer.WriteVarULong(Tick);
    }
}
