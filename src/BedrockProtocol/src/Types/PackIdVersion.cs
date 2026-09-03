using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class PackIdVersion : DataType {
    public Uuid PackUuid = new();
    public string PackVersion = string.Empty;

    public override void Write(ref BinaryWriter writer) {
        PackUuid.Write(ref writer);
        writer.WriteVarString(PackVersion);
    }

    public override void Read(ref BinaryReader reader) {
        PackUuid.Read(ref reader);
        PackVersion = reader.ReadVarString();
    }
}
