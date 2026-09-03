using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(29)]
public sealed class UpdateAttributesPacket : DataPacket {
    public ulong ActorRuntimeId;
    public AttributeData[] Attributes = [];
    public ulong Tick;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarULong(ActorRuntimeId);
        writer.WriteVarUInt((uint)Attributes.Length);
        for (int i = 0; i < Attributes.Length; i++) Attributes[i].Write(ref writer);
        writer.WriteVarULong(Tick);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ActorRuntimeId = reader.ReadVarULong();
        int count = checked((int)reader.ReadVarUInt());
        Attributes = new AttributeData[count];
        for (int i = 0; i < count; i++) {
            Attributes[i] = new AttributeData();
            Attributes[i].Read(ref reader);
        }
        Tick = reader.ReadVarULong();
    }
}
