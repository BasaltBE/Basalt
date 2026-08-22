using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(115)]
public sealed class NetworkStackLatencyPacket : DataPacket {
    public ulong CreationTime;
    public bool FromServer;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteUInt64(CreationTime, true);
        writer.WriteBool(FromServer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        CreationTime = reader.ReadUInt64(true);
        FromServer = reader.ReadBool();
    }
}
