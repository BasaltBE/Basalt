using System.Text;
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
    public string Identity = string.Empty;

    /// <summary>
    /// Client login payload. This is a JSON string containing additional client information such as device info, skin, language, etc.
    /// </summary>
    public string Client = string.Empty;

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteInt32(Protocol, false);

        int identityBytes = Encoding.UTF8.GetByteCount(Identity);
        int clientBytes = Encoding.UTF8.GetByteCount(Client);
        int connectionRequestLength = checked(sizeof(uint) + identityBytes + sizeof(uint) + clientBytes);

        writer.WriteVarUInt((uint)connectionRequestLength);
        writer.WriteString32(Identity, true);
        writer.WriteString32(Client, true);
    }

    public override void Deserialize(Binary.BinaryReader reader) {
        Protocol = reader.ReadInt32(false);

        int connectionRequestLength = checked((int)reader.ReadVarUInt());
        if (connectionRequestLength < 0 || connectionRequestLength > reader.Remaining)
            throw new InvalidOperationException("Invalid login connection request length.");

        Identity = reader.ReadString32(true);
        Client = reader.ReadString32(true);
    }
}
