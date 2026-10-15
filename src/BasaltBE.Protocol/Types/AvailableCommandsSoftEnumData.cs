using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class AvailableCommandsSoftEnumData : DataType {
    public string Name = string.Empty;
    public string[] Values = [];

    public override void Read(ref BinaryReader reader) {
        Name = reader.ReadVarString();
        Values = new string[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < Values.Length; index++)
            Values[index] = reader.ReadVarString();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteVarUInt((uint)Values.Length);
        foreach (string value in Values)
            writer.WriteVarString(value);
    }
}
