using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.NetworkSettings)]
public sealed record NetworkSettingsPacket : DataPacket {
    /// <summary>
    /// Compression threshold. 
    /// The size of the packet after which it should be compressed.
    /// </summary>
    public ushort CompressionThreshold;

    /// <summary>
    /// Compression method.
    /// The method used to compress packets that exceed the compression threshold.
    /// </summary>
    public CompressionMethod CompressionMethod;

    /// <summary>
    /// Client throttle.
    /// Whether the server should throttle the client if it sends too many packets in a short period of time.
    /// </summary>
    public bool ClientThrottle;

    /// <summary>
    /// Client throttle threshold.
    /// The number of packets that can be sent in a short period of time before the client is throttled.
    /// </summary>
    public byte ClientThrottleThreshold;

    /// <summary>
    /// Client throttle scalar.
    /// The factor by which the client's packet sending rate is reduced when throttled.
    /// </summary>
    public float ClientThrottleScalar;


    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteUInt16(CompressionThreshold, true);
        writer.WriteUInt16((ushort)CompressionMethod, true);
        writer.WriteBool(ClientThrottle);
        writer.WriteUInt8(ClientThrottleThreshold);
        writer.WriteF32(ClientThrottleScalar, true);
    }

    public override void Deserialize(Binary.BinaryReader reader) {
        CompressionThreshold = reader.ReadUInt16(true);
        CompressionMethod = (CompressionMethod)reader.ReadUInt16(true);
        ClientThrottle = reader.ReadBool();
        ClientThrottleThreshold = reader.ReadUInt8();
        ClientThrottleScalar = reader.ReadF32(true);
    }
}
