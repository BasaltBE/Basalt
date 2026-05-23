using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

/// <summary>
/// Parameter definition for a command overload.
/// </summary>
public sealed class CommandParameter : DataType
{
    /// <summary>
    /// Name shown for this command parameter.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Parameter type and flags.
    /// </summary>
    public uint Type;

    /// <summary>
    /// Whether this parameter may be omitted.
    /// </summary>
    public bool Optional;

    /// <summary>
    /// Extra parameter options.
    /// </summary>
    public byte Options;

    public void Read(BinaryReader reader)
    {
        Name = reader.ReadVarString();
        Type = reader.ReadUInt32(true);
        Optional = reader.ReadBool();
        Options = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarString(Name);
        writer.WriteUInt32(Type, true);
        writer.WriteBool(Optional);
        writer.WriteUInt8(Options);
    }
}
