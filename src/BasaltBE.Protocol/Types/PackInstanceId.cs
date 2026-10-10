using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class PackInstanceId : DataType {
    public string PackId = string.Empty;
    public string Version = string.Empty;
    public string SubpackName = string.Empty;

    public override void Read(ref BinaryReader reader) {
        PackId = reader.ReadVarString();
        Version = reader.ReadVarString();
        SubpackName = reader.ReadVarString();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(PackId);
        writer.WriteVarString(Version);
        writer.WriteVarString(SubpackName);
    }
}
