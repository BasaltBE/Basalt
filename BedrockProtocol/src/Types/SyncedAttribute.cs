using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class SyncedAttribute : DataType {
    public string Name = string.Empty;
    public float Minimum;
    public float Current;
    public float Maximum;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteF32(Minimum, true);
        writer.WriteF32(Current, true);
        writer.WriteF32(Maximum, true);
    }

    public override void Read(ref BinaryReader reader) {
        Name = reader.ReadVarString();
        Minimum = reader.ReadF32(true);
        Current = reader.ReadF32(true);
        Maximum = reader.ReadF32(true);
    }
}
