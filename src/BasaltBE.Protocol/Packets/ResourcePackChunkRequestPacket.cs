using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(84)]
public sealed class ResourcePackChunkRequestPacket : DataPacket {
    public string ResourceName = string.Empty;
    public int Chunk;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarString(ResourceName);
        writer.WriteInt32(Chunk, true);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ResourceName = reader.ReadVarString();
        Chunk = reader.ReadInt32(true);
    }
}
