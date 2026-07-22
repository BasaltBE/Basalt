using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

/// <summary>
/// Value entry used by a chained command subcommand.
/// </summary>
public sealed class ChainedSubcommandValue : DataType {
    /// <summary>
    /// Index into the AvailableCommands chained subcommand value table.
    /// </summary>
    public ushort Index;

    /// <summary>
    /// Argument type used for this chained subcommand value.
    /// </summary>
    public ushort Value;

    public void Read(BinaryReader reader) {
        Index = reader.ReadUInt16(true);
        Value = reader.ReadUInt16(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt16(Index, true);
        writer.WriteUInt16(Value, true);
    }
}
