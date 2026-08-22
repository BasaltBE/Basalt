using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ExperimentToggle : DataType {
    public string Name = string.Empty;
    public bool Enabled;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteBool(Enabled);
    }

    public override void Read(ref BinaryReader reader) {
        Name = reader.ReadVarString();
        Enabled = reader.ReadBool();
    }
}
