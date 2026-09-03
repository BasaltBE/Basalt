using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(58)]
public sealed class LevelChunkPacket : DataPacket {
    private const uint MaximumSubChunksCount = 64;

    public ChunkPos ChunkPosition = new();
    public int DimensionId;
    public uint SubChunksCount;
    public int? ClientRequestSubChunkLimit;
    public bool CacheEnabled;
    public SubChunkMetadata[] CacheMetadata = [];
    public byte[] RawPayload = [];

    public override void Serialize(ref BinaryWriter writer) {
        if (SubChunksCount > MaximumSubChunksCount) {
            throw new InvalidOperationException("Level chunk sub-chunk count exceeds 64.");
        }

        ChunkPosition.Write(ref writer);
        writer.WriteVarInt(DimensionId);
        writer.WriteVarUInt(SubChunksCount);
        writer.WriteBool(ClientRequestSubChunkLimit.HasValue);
        if (ClientRequestSubChunkLimit is int limit) writer.WriteVarInt(limit);
        writer.WriteBool(CacheEnabled);
        writer.WriteVarUInt((uint)CacheMetadata.Length);
        for (int i = 0; i < CacheMetadata.Length; i++) CacheMetadata[i].Write(ref writer);
        writer.WriteVarUInt((uint)RawPayload.Length);
        writer.WriteBytes(RawPayload);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ChunkPosition.Read(ref reader);
        DimensionId = reader.ReadVarInt();
        SubChunksCount = reader.ReadVarUInt();
        if (SubChunksCount > MaximumSubChunksCount) {
            throw new InvalidOperationException("Level chunk sub-chunk count exceeds 64.");
        }

        ClientRequestSubChunkLimit = reader.ReadBool() ? reader.ReadVarInt() : null;
        CacheEnabled = reader.ReadBool();
        int count = checked((int)reader.ReadVarUInt());
        CacheMetadata = new SubChunkMetadata[count];
        for (int i = 0; i < count; i++) {
            CacheMetadata[i] = new SubChunkMetadata();
            CacheMetadata[i].Read(ref reader);
        }
        int payloadLength = checked((int)reader.ReadVarUInt());
        RawPayload = reader.ReadBytes(payloadLength).ToArray();
    }
}
