using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(129)]
public sealed class ClientCacheStatusPacket : DataPacket {
    public bool SupportsCache;
    public override void Serialize(ref BinaryWriter writer) => writer.WriteBool(SupportsCache);
    public override void Deserialize(ref BinaryReader reader) => SupportsCache = reader.ReadBool();
}
