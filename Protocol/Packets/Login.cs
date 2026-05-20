using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.Login)]
public sealed record LoginPacket : DataPacket
{
    /// <summary>
    /// Protocol version.
    /// This is used to determine if client and server are compatible. 
    /// If the protocol versions mismatch, then they are on different mc versions.
    /// </summary>
    public int Protocol;

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteInt32(Protocol, false);
    }

    public override void Deserialize(Binary.BinaryReader reader)
    {
        Protocol = reader.ReadInt32(false);
    }
}
