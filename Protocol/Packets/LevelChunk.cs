using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.LevelChunk)]
public sealed record LevelChunkPacket : DataPacket
{
    public const uint SubChunkRequestModeLimitless = 0xFFFFFFFF;
    public const uint SubChunkRequestModeLimited = 0xFFFFFFFE;

    /// <summary>
    /// Chunk X coordinate.
    /// </summary>
    public int ChunkX;

    /// <summary>
    /// Chunk Z coordinate.
    /// </summary>
    public int ChunkZ;

    /// <summary>
    /// Dimension id.
    /// </summary>
    public int Dimension;

    /// <summary>
    /// Subchunk count indicator.
    /// </summary>
    public uint SubChunkCount;

    /// <summary>
    /// Highest subchunk when using limited mode.
    /// </summary>
    public ushort HighestSubChunk;

    /// <summary>
    /// Whether blob cache is enabled.
    /// </summary>
    public bool CacheEnabled;

    /// <summary>
    /// Blob hash list when cache is enabled.
    /// </summary>
    public List<ulong> BlobHashes = [];

    /// <summary>
    /// Raw serialized chunk payload.
    /// </summary>
    public byte[] RawPayload = [];

    public override void Deserialize(Binary.BinaryReader reader)
    {
        ChunkX = reader.ReadZigZag();
        ChunkZ = reader.ReadZigZag();
        Dimension = reader.ReadZigZag();
        SubChunkCount = reader.ReadVarUInt();

        if (SubChunkCount == SubChunkRequestModeLimited)
        {
            HighestSubChunk = reader.ReadUInt16(true);
        }

        CacheEnabled = reader.ReadBool();
        if (CacheEnabled)
        {
            int hashCount = checked((int)reader.ReadVarUInt());
            BlobHashes = new List<ulong>(hashCount);
            for (int i = 0; i < hashCount; i++)
            {
                BlobHashes.Add(reader.ReadUInt64());
            }
        }
        else
        {
            BlobHashes = [];
        }

        int payloadLength = checked((int)reader.ReadVarUInt());
        RawPayload = reader.ReadBytes(payloadLength).ToArray();
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteZigZag(ChunkX);
        writer.WriteZigZag(ChunkZ);
        writer.WriteZigZag(Dimension);
        writer.WriteVarUInt(SubChunkCount);

        if (SubChunkCount == SubChunkRequestModeLimited)
        {
            writer.WriteUInt16(HighestSubChunk, true);
        }

        writer.WriteBool(CacheEnabled);
        if (CacheEnabled)
        {
            writer.WriteVarUInt((uint)BlobHashes.Count);
            for (int i = 0; i < BlobHashes.Count; i++)
            {
                writer.WriteUInt64(BlobHashes[i]);
            }
        }

        writer.WriteVarUInt((uint)RawPayload.Length);
        writer.WriteBytes(RawPayload);
    }
}
