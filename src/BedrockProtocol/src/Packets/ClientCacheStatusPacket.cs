using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(129)]
public sealed class ClientCacheStatusPacket : DataPacket {
    public bool CacheSupported;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteBool(CacheSupported);
    }

    public override void Deserialize(ref BinaryReader reader) {
        CacheSupported = reader.ReadBool();
    }
}
