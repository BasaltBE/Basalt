using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

/// <summary>
/// Sent by the client to request the server's network settings
/// Includes the client's protocol version that is used to check if the 
/// server is compatible
/// </summary>
public sealed record RequestNetworkSettingsPacket : DataPacket
{
    public RequestNetworkSettingsPacket(int protocolVersion = 0)
    {
        ProtocolVersion = protocolVersion;
    }

    public int ProtocolVersion { get; set; }
    public override PacketId PacketId => PacketId.RequestNetworkSettings;

    public override void Deserialize(ref BinaryReader reader)
    {
        ProtocolVersion = reader.ReadInt32(false);
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteInt32(ProtocolVersion, false);
    }
}
