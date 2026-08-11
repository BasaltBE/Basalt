using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class LevelChunkPacketPayloadSubChunkMetadata {
    public ulong BlobId;

    public void Read(BinaryReader reader) {
        BlobId = reader.ReadUInt64(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt64(BlobId, true);
    }
}
