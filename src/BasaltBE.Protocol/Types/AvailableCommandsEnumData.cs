using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class AvailableCommandsEnumData : DataType {
    public string Name = string.Empty;
    public uint[] Values = [];

    public override void Read(ref BinaryReader reader) {
        Name = reader.ReadVarString();
        Values = new uint[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < Values.Length; index++)
            Values[index] = reader.ReadUInt32(true);
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteVarUInt((uint)Values.Length);
        foreach (uint value in Values)
            writer.WriteUInt32(value, true);
    }
}
