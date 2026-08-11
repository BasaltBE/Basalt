using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SubChunkPacketData {
    public SubChunkPosOffset SubChunkPosOffset = new();
    public SubChunkRequestResult SubChunkRequestResult;
    public string? SerializedSubChunk;
    public SubChunkHeightmapData HeightMapData = new();
    public ulong? BlobId;

    public void Read(BinaryReader reader) {
        SubChunkPosOffset.Read(reader);
        SubChunkRequestResult = (global::BedrockProtocol.Enums.SubChunkRequestResult)reader.ReadUInt8();
        if (reader.ReadBool()) {
            SerializedSubChunk = reader.ReadVarString();
        } else {
            SerializedSubChunk = default;
        }
        HeightMapData.Read(reader);
        if (reader.ReadBool()) {
            BlobId = reader.ReadUInt64(true);
        } else {
            BlobId = default;
        }
    }

    public void Write(BinaryWriter writer) {
        SubChunkPosOffset.Write(writer);
        writer.WriteUInt8((byte)SubChunkRequestResult);
        writer.WriteBool(SerializedSubChunk is not null);
        if (SerializedSubChunk is { } optionalValue5) {
            writer.WriteVarString(optionalValue5);
        }
        HeightMapData.Write(writer);
        writer.WriteBool(BlobId is not null);
        if (BlobId is { } optionalValue9) {
            writer.WriteUInt64(optionalValue9, true);
        }
    }
}
