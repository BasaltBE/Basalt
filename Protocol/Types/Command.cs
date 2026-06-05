using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

/// <summary>
/// Command definition sent to the client for help and autocomplete.
/// </summary>
public sealed class Command : DataType
{
    /// <summary>
    /// Command name shown and executed by the client.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Description shown in command help and autocomplete.
    /// </summary>
    public string Description = string.Empty;

    /// <summary>
    /// Command flags sent to the client.
    /// </summary>
    public ushort Flags;

    /// <summary>
    /// Permission level advertised for this command.
    /// </summary>
    public CommandPermissionLevel PermissionLevel;

    /// <summary>
    /// Offset to the aliases enum, if any.
    /// </summary>
    public uint AliasesOffset;

    /// <summary>
    /// Offsets into the AvailableCommands chained subcommand table.
    /// </summary>
    public List<ushort> ChainedSubcommandOffsets = [];

    /// <summary>
    /// Available overloads for this command.
    /// </summary>
    public List<CommandOverload> Overloads = [];

    public void Read(BinaryReader reader)
    {
        Name = reader.ReadVarString();
        Description = reader.ReadVarString();
        Flags = reader.ReadUInt16(true);
        PermissionLevel = PermissionFromString(reader.ReadVarString());
        AliasesOffset = reader.ReadUInt32(true);
        int chainedSubcommandCount = checked((int)reader.ReadVarUInt());
        ChainedSubcommandOffsets = new(chainedSubcommandCount);
        for (int i = 0; i < chainedSubcommandCount; i++)
        {
            ChainedSubcommandOffsets.Add(reader.ReadUInt16(true));
        }

        int overloadCount = checked((int)reader.ReadVarUInt());
        Overloads = new(overloadCount);
        for (int i = 0; i < overloadCount; i++)
        {
            CommandOverload overload = new();
            overload.Read(reader);
            Overloads.Add(overload);
        }
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarString(Name);
        writer.WriteVarString(Description);
        writer.WriteUInt16(Flags, true);
        writer.WriteVarString(PermissionToString(PermissionLevel));
        writer.WriteUInt32(AliasesOffset, true);
        writer.WriteVarUInt((uint)ChainedSubcommandOffsets.Count);
        for (int i = 0; i < ChainedSubcommandOffsets.Count; i++)
        {
            writer.WriteUInt16(ChainedSubcommandOffsets[i], true);
        }

        writer.WriteVarUInt((uint)Overloads.Count);
        for (int i = 0; i < Overloads.Count; i++)
        {
            Overloads[i].Write(writer);
        }
    }   


    /// <summary>
    ///  TODO! Add .toString or sum to the enum itself
    /// </summary>
    /// <param name="permissionLevel"></param>
    /// <returns></returns>
    static string PermissionToString(CommandPermissionLevel permissionLevel) => permissionLevel switch
    {
        CommandPermissionLevel.Any => "any",
        CommandPermissionLevel.GameDirectors => "gamedirectors",
        CommandPermissionLevel.Admin => "admin",
        CommandPermissionLevel.Host => "host",
        CommandPermissionLevel.Owner => "owner",
        CommandPermissionLevel.Internal => "internal",
        _ => "unknown"
    };

    static CommandPermissionLevel PermissionFromString(string permissionLevel) => permissionLevel switch
    {
        "any" => CommandPermissionLevel.Any,
        "gamedirectors" => CommandPermissionLevel.GameDirectors,
        "admin" => CommandPermissionLevel.Admin,
        "host" => CommandPermissionLevel.Host,
        "owner" => CommandPermissionLevel.Owner,
        "internal" => CommandPermissionLevel.Internal,
        _ => throw new InvalidOperationException($"Unknown command permission level: {permissionLevel}.")
    };
}
