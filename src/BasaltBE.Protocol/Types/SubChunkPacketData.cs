using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class SubChunkPacketData : DataType {
    public SubChunkPosOffset PositionOffset = new();
    public SubChunkRequestResult Result;
    public ReadOnlyMemory<byte> SerializedSubChunk;
    public SubChunkHeightmapData Heightmap = new();
    public ulong? BlobId;

    public override void Read(ref BinaryReader reader) => Read(ref reader, false);

    internal void Read(ref BinaryReader reader, bool cacheEnabled) {
        PositionOffset.Read(ref reader);
        Result = (SubChunkRequestResult)reader.ReadUInt8();
        if (!Enum.IsDefined(Result))
            throw new InvalidDataException("Subchunk request result is invalid.");

        if (!(cacheEnabled && Result == SubChunkRequestResult.SuccessAllAir) && reader.ReadBool()) {
            uint length = reader.ReadVarUInt();
            if (length > reader.Remaining)
                throw new InvalidDataException("Subchunk data length exceeds the remaining packet data.");
            SerializedSubChunk = reader.ReadBytes((int)length).ToArray();
        }

        Heightmap.Read(ref reader);
        if (cacheEnabled && Result is (SubChunkRequestResult.Success or SubChunkRequestResult.SuccessAllAir) && reader.ReadBool())
            BlobId = reader.ReadUInt64(true);
    }

    public override void Write(ref BinaryWriter writer) => Write(ref writer, false);

    internal void Write(ref BinaryWriter writer, bool cacheEnabled) {
        ArgumentNullException.ThrowIfNull(PositionOffset);
        ArgumentNullException.ThrowIfNull(Heightmap);
        if (!Enum.IsDefined(Result))
            throw new InvalidDataException("Subchunk request result is invalid.");

        PositionOffset.Write(ref writer);
        writer.WriteUInt8((byte)Result);
        if (!(cacheEnabled && Result == SubChunkRequestResult.SuccessAllAir)) {
            writer.WriteBool(!SerializedSubChunk.IsEmpty);
            if (!SerializedSubChunk.IsEmpty) {
                writer.WriteVarUInt((uint)SerializedSubChunk.Length);
                writer.WriteBytes(SerializedSubChunk.Span);
            }
        }

        Heightmap.Write(ref writer);
        if (cacheEnabled && Result is (SubChunkRequestResult.Success or SubChunkRequestResult.SuccessAllAir)) {
            writer.WriteBool(BlobId.HasValue);
            if (BlobId is ulong blobId)
                writer.WriteUInt64(blobId, true);
        }
    }
}
