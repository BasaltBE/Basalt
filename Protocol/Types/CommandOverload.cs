using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

/// <summary>
/// One supported argument layout for a command.
/// </summary>
public sealed class CommandOverload : DataType {
    /// <summary>
    /// Whether this overload uses chained subcommands.
    /// </summary>
    public bool Chaining;

    /// <summary>
    /// Parameters that make up this command overload.
    /// </summary>
    public List<CommandParameter> Parameters = [];

    public void Read(BinaryReader reader) {
        Chaining = reader.ReadBool();
        int count = checked((int)reader.ReadVarUInt());
        Parameters = new(count);
        for (int i = 0; i < count; i++) {
            CommandParameter parameter = new();
            parameter.Read(reader);
            Parameters.Add(parameter);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteBool(Chaining);
        writer.WriteVarUInt((uint)Parameters.Count);
        for (int i = 0; i < Parameters.Count; i++) {
            Parameters[i].Write(writer);
        }
    }
}
