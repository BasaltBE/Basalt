using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using ProtoAttribute = Basalt.Protocol.Types.Attribute;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.UpdateAttributes)]
public sealed record UpdateAttributesPacket : DataPacket
{
    /// <summary>
    /// Runtime id of the actor.
    /// </summary>
    public ulong RuntimeId;

    /// <summary>
    /// Attribute values to update.
    /// </summary>
    public List<ProtoAttribute> Attributes = [];

    /// <summary>
    /// Server tick for this update.
    /// </summary>
    public ulong Tick;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        RuntimeId = unchecked((ulong)reader.ReadVarLong());
        Attributes = ProtoAttribute.ReadList(reader);
        Tick = unchecked((ulong)reader.ReadVarLong());
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarLong(unchecked((long)RuntimeId));
        ProtoAttribute.WriteList(writer, Attributes);
        writer.WriteVarLong(unchecked((long)Tick));
    }
}
