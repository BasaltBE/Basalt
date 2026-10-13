using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(121)]
public sealed class NetworkChunkPublisherUpdatePacket : DataPacket {
    public BlockPos Position = new();
    public uint Radius;
    public ChunkPos[] ServerBuiltChunks = [];

    public override void Serialize(ref BinaryWriter writer) {
        ArgumentNullException.ThrowIfNull(Position);
        if (ServerBuiltChunks.Length > 9216) {
            throw new InvalidDataException("The chunk publisher packet cannot contain more than 9216 built chunks.");
        }

        Position.Write(ref writer);
        writer.WriteVarUInt(Radius);
        writer.WriteUInt32((uint)ServerBuiltChunks.Length, true);
        foreach (ChunkPos position in ServerBuiltChunks) {
            ArgumentNullException.ThrowIfNull(position);
            position.Write(ref writer);
        }
    }

    public override void Deserialize(ref BinaryReader reader) {
        Position.Read(ref reader);
        Radius = reader.ReadVarUInt();
        uint chunkCount = reader.ReadUInt32(true);
        if (chunkCount > 9216) {
            throw new InvalidDataException("The chunk publisher packet cannot contain more than 9216 built chunks.");
        }

        ServerBuiltChunks = new ChunkPos[(int)chunkCount];
        for (int i = 0; i < ServerBuiltChunks.Length; i++) {
            ChunkPos position = new();
            position.Read(ref reader);
            ServerBuiltChunks[i] = position;
        }
    }
}
