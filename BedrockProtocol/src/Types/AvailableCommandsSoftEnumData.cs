using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class AvailableCommandsSoftEnumData : DataType {
    public string Name = string.Empty;
    public string[] Options = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteVarUInt((uint)Options.Length);
        for (int i = 0; i < Options.Length; i++) writer.WriteVarString(Options[i]);
    }

    public override void Read(ref BinaryReader reader) {
        Name = reader.ReadVarString();
        int count = checked((int)reader.ReadVarUInt());
        Options = new string[count];
        for (int i = 0; i < count; i++) Options[i] = reader.ReadVarString();
    }
}
