using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

/// <summary>
/// Fixed enum definition used by available commands.
/// </summary>
public sealed class CommandEnum : DataType
{
    /// <summary>
    /// Type name shown for this enum in command usage.
    /// </summary>
    public string Type = string.Empty;

    /// <summary>
    /// Indices into the AvailableCommands enum value table.
    /// </summary>
    public List<uint> ValueIndices = [];

    public void Read(BinaryReader reader)
    {
        Type = reader.ReadVarString();
        int count = checked((int)reader.ReadVarUInt());
        ValueIndices = new(count);
        for (int i = 0; i < count; i++)
        {
            ValueIndices.Add(reader.ReadUInt32(true));
        }
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarString(Type);
        writer.WriteVarUInt((uint)ValueIndices.Count);
        for (int i = 0; i < ValueIndices.Count; i++)
        {
            writer.WriteUInt32(ValueIndices[i], true);
        }
    }
}
