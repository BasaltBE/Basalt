using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(193)]
public sealed class RequestNetworkSettingsPacket : DataPacket {
    public int ClientNetworkVersion;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteInt32(ClientNetworkVersion, false);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ClientNetworkVersion = reader.ReadInt32(false);
    }
}
