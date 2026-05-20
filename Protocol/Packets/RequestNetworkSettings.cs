using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.RequestNetworkSettings)]
public sealed record RequestNetworkSettingsPacket : DataPacket
{   
    /// <summary>
    /// Protocol version.
    /// This is used to determine if client and server are compatible. 
    /// If the protocol versions mismatch, then they are on different mc versions.
    /// </summary>
    public int Protocol;

    public RequestNetworkSettingsPacket() : this(1001)
    {
    }

    public RequestNetworkSettingsPacket(int protocol)
    {
        Protocol = protocol;
    }

    public override void Deserialize(Basalt.Binary.BinaryReader reader)
    {
        Protocol = reader.ReadInt32(false);
    }

    public override void Serialize(Basalt.Binary.BinaryWriter writer)
    {
        writer.WriteInt32(Protocol, false);
    }
};
