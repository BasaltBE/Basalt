using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

/// <summary>
/// Constraint applied to a command enum value.
/// </summary>
public sealed class CommandEnumConstraint : DataType
{
    /// <summary>
    /// Index of the enum value this constraint applies to.
    /// </summary>
    public uint EnumValueIndex;

    /// <summary>
    /// Index of the enum this constraint applies to.
    /// </summary>
    public uint EnumIndex;

    /// <summary>
    /// Constraint ids applied to the enum value.
    /// </summary>
    public List<byte> Constraints = [];

    public void Read(BinaryReader reader)
    {
        EnumValueIndex = reader.ReadUInt32(true);
        EnumIndex = reader.ReadUInt32(true);
        int count = checked((int)reader.ReadVarUInt());
        Constraints = new(count);
        for (int i = 0; i < count; i++)
        {
            Constraints.Add(reader.ReadUInt8());
        }
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteUInt32(EnumValueIndex, true);
        writer.WriteUInt32(EnumIndex, true);
        writer.WriteVarUInt((uint)Constraints.Count);
        for (int i = 0; i < Constraints.Count; i++)
        {
            writer.WriteUInt8(Constraints[i]);
        }
    }
}
