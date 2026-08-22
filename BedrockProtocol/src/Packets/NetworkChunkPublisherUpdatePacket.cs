using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(121)]
public sealed class NetworkChunkPublisherUpdatePacket : DataPacket {
    public BlockPos Position = new();
    public uint Radius;
    public ChunkPos[] ServerBuiltChunks = [];

    public override void Serialize(ref BinaryWriter writer) {
        Position.Write(ref writer);
        writer.WriteVarUInt(Radius);
        if (ServerBuiltChunks.Length > 9216) {
            throw new InvalidOperationException("Server built chunks exceeds the maximum length of 9216.");
        }

        writer.WriteUInt32((uint)ServerBuiltChunks.Length, true);
        for (int i = 0; i < ServerBuiltChunks.Length; i++) ServerBuiltChunks[i].Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Position.Read(ref reader);
        Radius = reader.ReadVarUInt();
        int count = checked((int)reader.ReadUInt32(true));
        ServerBuiltChunks = new ChunkPos[count];
        for (int i = 0; i < count; i++) {
            ServerBuiltChunks[i] = new ChunkPos();
            ServerBuiltChunks[i].Read(ref reader);
        }
    }
}
