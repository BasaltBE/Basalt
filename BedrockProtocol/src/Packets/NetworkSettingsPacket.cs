using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(143)]
public sealed class NetworkSettingsPacket : DataPacket {
    public ushort CompressionThreshold;
    public CompressionAlgorithm CompressionAlgorithm;
    public bool ClientThrottleEnabled;
    public byte ClientThrottleThreshold;
    public float ClientThrottleScalar;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteUInt16(CompressionThreshold, true);
        writer.WriteUInt16((ushort)CompressionAlgorithm, true);
        writer.WriteBool(ClientThrottleEnabled);
        writer.WriteUInt8(ClientThrottleThreshold);
        writer.WriteF32(ClientThrottleScalar, true);
    }

    public override void Deserialize(ref BinaryReader reader) {
        CompressionThreshold = reader.ReadUInt16(true);
        CompressionAlgorithm = (CompressionAlgorithm)reader.ReadUInt16(true);
        ClientThrottleEnabled = reader.ReadBool();
        ClientThrottleThreshold = reader.ReadUInt8();
        ClientThrottleScalar = reader.ReadF32(true);
    }
}
