using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class AvailableCommandsEnumData : DataType {
    public string Name = string.Empty;
    public uint[] Values = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteVarUInt((uint)Values.Length);
        for (int i = 0; i < Values.Length; i++) writer.WriteUInt32(Values[i], true);
    }

    public override void Read(ref BinaryReader reader) {
        Name = reader.ReadVarString();
        int count = checked((int)reader.ReadVarUInt());
        Values = new uint[count];
        for (int i = 0; i < count; i++) Values[i] = reader.ReadUInt32(true);
    }
}
