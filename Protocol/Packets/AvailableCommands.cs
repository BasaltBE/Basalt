using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

/// <summary>
/// Sends all commands available to the client.
/// </summary>
[Packet(PacketId.AvailableCommands)]
public sealed record AvailableCommandsPacket : DataPacket
{
    /// <summary>
    /// Shared enum values referenced by command enums.
    /// </summary>
    public List<string> EnumValues = [];

    /// <summary>
    /// Shared chained subcommand values referenced by chained subcommands.
    /// </summary>
    public List<string> ChainedSubcommandValues = [];

    /// <summary>
    /// Shared suffix values referenced by command parameters.
    /// </summary>
    public List<string> Suffixes = [];

    /// <summary>
    /// Fixed command enums available to commands.
    /// </summary>
    public List<CommandEnum> Enums = [];

    /// <summary>
    /// Chained subcommands available to commands.
    /// </summary>
    public List<ChainedSubcommand> ChainedSubcommands = [];

    /// <summary>
    /// Commands shown to the client for help and autocomplete.
    /// </summary>
    public List<Command> Commands = [];

    /// <summary>
    /// Dynamic command enums that may be changed at runtime.
    /// </summary>
    public List<DynamicEnum> DynamicEnums = [];

    /// <summary>
    /// Constraints applied to command enum values.
    /// </summary>
    public List<CommandEnumConstraint> Constraints = [];

    public override void Deserialize(Binary.BinaryReader reader)
    {
        EnumValues = ReadStringList(reader);
        ChainedSubcommandValues = ReadStringList(reader);
        Suffixes = ReadStringList(reader);
        Enums = ReadList<CommandEnum>(reader);
        ChainedSubcommands = ReadList<ChainedSubcommand>(reader);
        Commands = ReadList<Command>(reader);
        DynamicEnums = ReadList<DynamicEnum>(reader);
        Constraints = ReadList<CommandEnumConstraint>(reader);
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        WriteStringList(writer, EnumValues);
        WriteStringList(writer, ChainedSubcommandValues);
        WriteStringList(writer, Suffixes);
        WriteList(writer, Enums);
        WriteList(writer, ChainedSubcommands);
        WriteList(writer, Commands);
        WriteList(writer, DynamicEnums);
        WriteList(writer, Constraints);
    }

    static List<string> ReadStringList(Binary.BinaryReader reader)
    {
        int count = checked((int)reader.ReadVarUInt());
        List<string> values = new(count);
        for (int i = 0; i < count; i++)
        {
            values.Add(reader.ReadVarString());
        }

        return values;
    }

    static List<T> ReadList<T>(Binary.BinaryReader reader) where T : DataType, new()
    {
        int count = checked((int)reader.ReadVarUInt());
        List<T> values = new(count);
        for (int i = 0; i < count; i++)
        {
            T value = new();
            value.Read(reader);
            values.Add(value);
        }

        return values;
    }

    static void WriteStringList(Binary.BinaryWriter writer, List<string> values)
    {
        writer.WriteVarUInt((uint)values.Count);
        for (int i = 0; i < values.Count; i++)
        {
            writer.WriteVarString(values[i]);
        }
    }

    static void WriteList<T>(Binary.BinaryWriter writer, List<T> values) where T : DataType
    {
        writer.WriteVarUInt((uint)values.Count);
        for (int i = 0; i < values.Count; i++)
        {
            values[i].Write(writer);
        }
    }
}
