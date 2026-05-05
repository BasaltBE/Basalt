using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record NetworkSettingsPacket : DataPacket
{
    public NetworkSettingsPacket() {}

    public NetworkSettingsPacket(
        ushort compressionThreshold,
        CompressionMethod compressionMethod,
        bool clientThrottle,
        byte clientThrottleThreshold,
        float clientThrottleScalar)
    {
        CompressionThreshold = compressionThreshold;
        CompressionMethod = compressionMethod;
        ClientThrottle = clientThrottle;
        ClientThrottleThreshold = clientThrottleThreshold;
        ClientThrottleScalar = clientThrottleScalar;
    }

    public ushort CompressionThreshold { get; set; }
    public CompressionMethod CompressionMethod { get; set; }
    public bool ClientThrottle { get; set; }
    public byte ClientThrottleThreshold { get; set; }
    public float ClientThrottleScalar { get; set; }

    public override PacketId PacketId => PacketId.NetworkSettings;

    public override void Deserialize(ref BinaryReader reader)
    {
        CompressionThreshold = reader.ReadUInt16(true);
        CompressionMethod = (CompressionMethod)reader.ReadUInt16(true);
        ClientThrottle = reader.ReadBool();
        ClientThrottleThreshold = reader.ReadUInt8();
        ClientThrottleScalar = reader.ReadF32(true);
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteUInt16(CompressionThreshold, true);
        writer.WriteUInt16((ushort)CompressionMethod, true);
        writer.WriteBool(ClientThrottle);
        writer.WriteUInt8(ClientThrottleThreshold);
        writer.WriteF32(ClientThrottleScalar, true);
    }
}
