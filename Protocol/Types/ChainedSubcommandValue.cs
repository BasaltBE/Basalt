using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

/// <summary>
/// Value entry used by a chained command subcommand.
/// </summary>
public sealed class ChainedSubcommandValue : DataType
{
    /// <summary>
    /// Index into the AvailableCommands chained subcommand value table.
    /// </summary>
    public uint Index;

    /// <summary>
    /// Argument type used for this chained subcommand value.
    /// </summary>
    public uint Value;

    public void Read(BinaryReader reader)
    {
        Index = reader.ReadVarUInt();
        Value = reader.ReadVarUInt();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarUInt(Index);
        writer.WriteVarUInt(Value);
    }
}
