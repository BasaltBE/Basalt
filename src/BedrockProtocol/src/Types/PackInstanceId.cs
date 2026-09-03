using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class PackInstanceId : DataType {
    public string PackId = string.Empty;
    public string Version = string.Empty;
    public string SubPackName = string.Empty;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(PackId);
        writer.WriteVarString(Version);
        writer.WriteVarString(SubPackName);
    }

    public override void Read(ref BinaryReader reader) {
        PackId = reader.ReadVarString();
        Version = reader.ReadVarString();
        SubPackName = reader.ReadVarString();
    }
}
