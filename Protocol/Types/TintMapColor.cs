using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class TintMapColor : DataType {
    /// <summary>The colors assigned to the persona piece.</summary>
    public List<string> Colors = [];

    public void Read(BinaryReader reader) {
        int count = checked((int)reader.ReadVarUInt());
        Colors = new List<string>(count);
        for (int i = 0; i < count; i++) {
            Colors.Add(reader.ReadVarString());
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt((uint)Colors.Count);
        for (int i = 0; i < Colors.Count; i++) {
            writer.WriteVarString(Colors[i]);
        }
    }
}
