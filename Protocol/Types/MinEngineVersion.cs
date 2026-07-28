using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class MinEngineVersion : DataType {
    /// <summary>The minimum engine version string for the geometry.</summary>
    public string Value = string.Empty;

    public void Read(BinaryReader reader) => Value = reader.ReadVarString();

    public void Write(BinaryWriter writer) => writer.WriteVarString(Value);
}
