using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

/// <summary>
/// Chained subcommand definition used by available commands.
/// </summary>
public sealed class ChainedSubcommand : DataType {
    /// <summary>
    /// Name of the chained subcommand.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Values accepted by the chained subcommand.
    /// </summary>
    public List<ChainedSubcommandValue> Values = [];

    public void Read(BinaryReader reader) {
        Name = reader.ReadVarString();
        int count = checked((int)reader.ReadVarUInt());
        Values = new(count);
        for (int i = 0; i < count; i++) {
            ChainedSubcommandValue value = new();
            value.Read(reader);
            Values.Add(value);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteVarUInt((uint)Values.Count);
        for (int i = 0; i < Values.Count; i++) {
            Values[i].Write(writer);
        }
    }
}
