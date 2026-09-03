using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(70)]
public class ChunkRadiusUpdatedPacket : DataPacket {
    public int ChunkRadius;

    public override void Serialize(ref BinaryWriter writer) => writer.WriteZigZong(ChunkRadius);
    public override void Deserialize(ref BinaryReader reader) => ChunkRadius = checked((int)reader.ReadZigZong());
}
