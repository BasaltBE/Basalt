using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

/// <summary>
/// Command enum whose values may be updated at runtime.
/// </summary>
public sealed class DynamicEnum : DataType {
    /// <summary>
    /// Type name of the dynamic enum.
    /// </summary>
    public string Type = string.Empty;

    /// <summary>
    /// Values currently available for the dynamic enum.
    /// </summary>
    public List<string> Values = [];

    public void Read(BinaryReader reader) {
        Type = reader.ReadVarString();
        int count = checked((int)reader.ReadVarUInt());
        Values = new(count);
        for (int i = 0; i < count; i++) {
            Values.Add(reader.ReadVarString());
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Type);
        writer.WriteVarUInt((uint)Values.Count);
        for (int i = 0; i < Values.Count; i++) {
            writer.WriteVarString(Values[i]);
        }
    }
}
