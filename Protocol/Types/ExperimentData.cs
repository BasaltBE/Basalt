using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ExperimentData : DataType
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }

    public void Read(BinaryReader reader)
    {
        Name = reader.ReadVarString();
        Enabled = reader.ReadBool();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarString(Name);
        writer.WriteBool(Enabled);
    }
}


