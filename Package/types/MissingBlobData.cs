#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class MissingBlobData {
    public ulong BlobId;
    public string BlobData = string.Empty;

    public void Read(BinaryReader reader) {
        BlobId = reader.ReadUInt64(true);
        BlobData = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt64(BlobId, true);
        writer.WriteVarString(BlobData);
    }
}
