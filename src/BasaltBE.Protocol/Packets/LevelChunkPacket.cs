using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(58)]
public sealed class LevelChunkPacket : DataPacket {
    public ChunkPos Position = new();
    public int DimensionId;
    public uint SubChunkCount;
    public int? ClientRequestSubChunkLimit;
    public bool CacheEnabled;
    public ulong[] CacheMetadata = [];
    public ReadOnlyMemory<byte> SerializedChunkData;
    public bool IsClientBiomeUpdate;

    public override void Serialize(ref BinaryWriter writer) {
        ArgumentNullException.ThrowIfNull(Position);
        if (SubChunkCount > 64) {
            throw new InvalidDataException("Level chunk subchunk count cannot exceed 64.");
        }
        if (ClientRequestSubChunkLimit is < -1 or > 64) {
            throw new InvalidDataException("Client request subchunk limit must be from -1 to 64.");
        }
        if (CacheMetadata.Length > 65) {
            throw new InvalidDataException("Level chunk cache metadata cannot contain more than 65 entries.");
        }

        Position.Write(ref writer);
        writer.WriteZigZag(DimensionId);
        writer.WriteVarUInt(SubChunkCount);
        writer.WriteBool(ClientRequestSubChunkLimit.HasValue);
        if (ClientRequestSubChunkLimit is int clientRequestSubChunkLimit) {
            writer.WriteZigZag(clientRequestSubChunkLimit);
        }
        writer.WriteBool(CacheEnabled);
        writer.WriteVarUInt((uint)CacheMetadata.Length);
        foreach (ulong blobId in CacheMetadata) {
            writer.WriteUInt64(blobId, true);
        }
        writer.WriteVarUInt((uint)SerializedChunkData.Length);
        writer.WriteBytes(SerializedChunkData.Span);
        writer.WriteBool(IsClientBiomeUpdate);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Position.Read(ref reader);
        DimensionId = reader.ReadZigZag();
        SubChunkCount = reader.ReadVarUInt();
        if (SubChunkCount > 64) {
            throw new InvalidDataException("Level chunk subchunk count cannot exceed 64.");
        }

        ClientRequestSubChunkLimit = reader.ReadBool() ? reader.ReadZigZag() : null;
        if (ClientRequestSubChunkLimit is < -1 or > 64) {
            throw new InvalidDataException("Client request subchunk limit must be from -1 to 64.");
        }

        CacheEnabled = reader.ReadBool();
        uint metadataCount = reader.ReadVarUInt();
        if (metadataCount > 65) {
            throw new InvalidDataException("Level chunk cache metadata cannot contain more than 65 entries.");
        }
        CacheMetadata = new ulong[(int)metadataCount];
        for (int i = 0; i < CacheMetadata.Length; i++) {
            CacheMetadata[i] = reader.ReadUInt64(true);
        }

        uint dataLength = reader.ReadVarUInt();
        if (dataLength > reader.Remaining) {
            throw new InvalidDataException("Level chunk data length exceeds the remaining packet data.");
        }
        SerializedChunkData = reader.ReadBytes((int)dataLength).ToArray();
        IsClientBiomeUpdate = reader.ReadBool();
    }
}
