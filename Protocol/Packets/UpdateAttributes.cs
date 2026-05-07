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
        RuntimeId = unchecked((ulong)reader.ReadVarLong());
        Attributes = ProtoAttribute.ReadList(ref reader);
        Tick = unchecked((ulong)reader.ReadVarLong());
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteVarLong(unchecked((long)RuntimeId));
        ProtoAttribute.WriteList(ref writer, Attributes);
        writer.WriteVarLong(unchecked((long)Tick));
    }
}
