using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ExperimentData : DataType
{
    /// <summary>
    /// The name of the experiment
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Whether the experiment is enabled or not
    /// </summary>
    public bool Enabled;

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


