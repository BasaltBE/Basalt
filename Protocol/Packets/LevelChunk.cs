using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record LevelChunkPacket : DataPacket
{
    public const uint SubChunkRequestModeLimitless = 0xFFFFFFFF;
    public const uint SubChunkRequestModeLimited = 0xFFFFFFFE;

    public int ChunkX { get; set; }
    public int ChunkZ { get; set; }
    public int Dimension { get; set; }
    public uint SubChunkCount { get; set; }
    public ushort HighestSubChunk { get; set; }
    public bool CacheEnabled { get; set; }
    public List<ulong> BlobHashes { get; set; } = [];
    public byte[] RawPayload { get; set; } = [];

    public override PacketId PacketId => PacketId.LevelChunk;

    public override void Deserialize(ref BinaryReader reader)
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

    public override void Serialize(ref BinaryWriter writer)
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
