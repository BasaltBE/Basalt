using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(70)]
public sealed class ChunkRadiusUpdatedPacket : DataPacket {
    public int ChunkRadius;

    public override void Serialize(ref BinaryWriter writer) => writer.WriteZigZag(ChunkRadius);

    public override void Deserialize(ref BinaryReader reader) => ChunkRadius = reader.ReadZigZag();
}
