using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(156)]
public sealed class PacketViolationWarningPacket : DataPacket {
    public PacketViolationType Type;
    public PacketViolationSeverity Severity;
    public int PacketId;
    public string Context = string.Empty;

    public override void Deserialize(ref BinaryReader reader) {
        Type = (PacketViolationType)reader.ReadVarInt();
        Severity = (PacketViolationSeverity)reader.ReadVarInt();
        PacketId = reader.ReadVarInt();
        Context = reader.ReadVarString();
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarInt((int)Type);
        writer.WriteVarInt((int)Severity);
        writer.WriteVarInt(PacketId);
        writer.WriteVarString(Context);
    }
}
