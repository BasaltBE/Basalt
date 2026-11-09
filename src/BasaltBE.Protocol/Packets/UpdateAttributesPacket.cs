using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(29)]
public sealed class UpdateAttributesPacket : DataPacket {
    public ulong ActorRuntimeId;
    public AttributeData[] Attributes = [];
    public ulong Tick;

    public override void Deserialize(ref BinaryReader reader) {
        ActorRuntimeId = reader.ReadVarULong();
        Attributes = new AttributeData[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < Attributes.Length; i++) {
            AttributeData attribute = new();
            attribute.Read(ref reader);
            Attributes[i] = attribute;
        }
        Tick = reader.ReadVarULong();
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarULong(ActorRuntimeId);
        writer.WriteVarUInt((uint)Attributes.Length);
        foreach (AttributeData attribute in Attributes)
            attribute.Write(ref writer);
        writer.WriteVarULong(Tick);
    }
}
