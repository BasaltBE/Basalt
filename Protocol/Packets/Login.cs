using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.Login)]
public sealed record LoginPacket : DataPacket {
    /// <summary>
    /// Protocol version.
    /// This is used to determine if client and server are compatible. 
    /// If the protocol versions mismatch, then they are on different mc versions.
    /// </summary>
    public int Protocol;

    /// <summary>
    /// Client login identity. This is a JWT token containing client information and authentication data.
    /// </summary>
    public byte[] ConnectionRequest = [];

    /// <summary>
    /// Client login payload. This is a JSON string containing additional client information such as device info, skin, language, etc.
    /// </summary>
    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteInt32(Protocol, false);
        writer.WriteVarUInt((uint)ConnectionRequest.Length);
        writer.WriteBytes(ConnectionRequest);
    }

    public override void Deserialize(Binary.BinaryReader reader) {
        Protocol = reader.ReadInt32(false);

        int connectionRequestLength = checked((int)reader.ReadVarUInt());
        if (connectionRequestLength < 0 || connectionRequestLength > reader.Remaining)
            throw new InvalidOperationException("Invalid login connection request length.");

        ConnectionRequest = reader.ReadBytes(connectionRequestLength).ToArray();
    }
}
