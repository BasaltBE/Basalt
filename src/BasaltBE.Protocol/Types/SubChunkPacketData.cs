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

    public override void Read(ref BinaryReader reader) {
        PositionOffset.Read(ref reader);
        byte result = reader.ReadUInt8();
        Result = (SubChunkRequestResult)result;
        if (!Enum.IsDefined(Result))
            throw new InvalidDataException($"Subchunk request result {result} is invalid.");

        if (reader.ReadBool()) {
            uint length = reader.ReadVarUInt();
            if (length > reader.Remaining)
                throw new InvalidDataException("Subchunk data length exceeds the remaining packet data.");
            SerializedSubChunk = reader.ReadBytes((int)length).ToArray();
        }

        Heightmap.Read(ref reader);
        if (reader.ReadBool())
            BlobId = reader.ReadUInt64(true);
    }

    public override void Write(ref BinaryWriter writer) {
        ArgumentNullException.ThrowIfNull(PositionOffset);
        ArgumentNullException.ThrowIfNull(Heightmap);
        if (!Enum.IsDefined(Result))
            throw new InvalidDataException("Subchunk request result is invalid.");

        PositionOffset.Write(ref writer);
        writer.WriteUInt8((byte)Result);
        writer.WriteBool(!SerializedSubChunk.IsEmpty);
        if (!SerializedSubChunk.IsEmpty) {
            writer.WriteVarUInt((uint)SerializedSubChunk.Length);
            writer.WriteBytes(SerializedSubChunk.Span);
        }

        Heightmap.Write(ref writer);
        writer.WriteBool(BlobId.HasValue);
        if (BlobId is ulong blobId)
            writer.WriteUInt64(blobId, true);
    }
}
