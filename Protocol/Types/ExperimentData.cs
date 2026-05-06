using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ExperimentData
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }

    public void Read(ref BinaryReader reader)
    {
        Name = reader.ReadVarString();
        Enabled = reader.ReadBool();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarString(Name);
        writer.WriteBool(Enabled);
    }
}

