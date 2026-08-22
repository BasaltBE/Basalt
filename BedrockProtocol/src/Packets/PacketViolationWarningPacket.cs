using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(156)]
public sealed class PacketViolationWarningPacket : DataPacket {
    public PacketViolationType ViolationType;
    public PacketViolationSeverity ViolationSeverity;
    public int ViolationPacketId;
    public string ViolationContext = string.Empty;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteZigZag((int)ViolationType);
        writer.WriteZigZag((int)ViolationSeverity);
        writer.WriteZigZag(ViolationPacketId);
        writer.WriteVarString(ViolationContext);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ViolationType = (PacketViolationType)reader.ReadZigZag();
        ViolationSeverity = (PacketViolationSeverity)reader.ReadZigZag();
        ViolationPacketId = reader.ReadZigZag();
        ViolationContext = reader.ReadVarString();
    }
}
