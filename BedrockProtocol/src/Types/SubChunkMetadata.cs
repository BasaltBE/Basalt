using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class SubChunkMetadata : DataType {
    public ulong BlobId;

    public override void Write(ref BinaryWriter writer) => writer.WriteUInt64(BlobId, true);
    public override void Read(ref BinaryReader reader) => BlobId = reader.ReadUInt64(true);
}
