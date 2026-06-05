using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

/// <summary>
/// Origin information for a command request or response.
/// </summary>
public sealed class CommandOrigin : DataType
{
    /// <summary>
    /// Source type that requested the command.
    /// </summary>
    public CommandOriginType Origin;

    /// <summary>
    /// Unique command origin identifier.
    /// </summary>
    public Guid UUID;

    /// <summary>
    /// Request id used to match responses to the command request.
    /// </summary>
    public string RequestId = string.Empty;

    /// <summary>
    /// Player unique id for origins that include a player context.
    /// </summary>
    public long PlayerUniqueId;

    public void Read(BinaryReader reader)
    {
        Origin = OriginFromString(reader.ReadVarString());
        UUID = Basalt.Protocol.Types.UUID.Read(reader);
        RequestId = reader.ReadVarString();
        PlayerUniqueId = reader.ReadInt64(true);
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarString(OriginToString(Origin));
        Basalt.Protocol.Types.UUID.Write(writer, UUID);
        writer.WriteVarString(RequestId);
        writer.WriteInt64(PlayerUniqueId, true);
    }

    static string OriginToString(CommandOriginType origin) => origin switch
    {
        CommandOriginType.Player => "player",
        CommandOriginType.CommandBlock => "commandblock",
        CommandOriginType.MinecartCommandBlock => "minecartcommandblock",
        CommandOriginType.DevConsole => "devconsole",
        CommandOriginType.Test => "test",
        CommandOriginType.AutomationPlayer => "automationplayer",
        CommandOriginType.ClientAutomation => "clientautomation",
        CommandOriginType.DedicatedServer => "dedicatedserver",
        CommandOriginType.Entity => "entity",
        CommandOriginType.Virtual => "virtual",
        CommandOriginType.GameArgument => "gameargument",
        CommandOriginType.EntityServer => "entityserver",
        CommandOriginType.Precompiled => "precompiled",
        CommandOriginType.GameDirectorEntityServer => "gamedirectorentityserver",
        CommandOriginType.Script => "scripting",
        CommandOriginType.Executor => "executecontext",
        _ => "unknown"
    };

    static CommandOriginType OriginFromString(string origin) => origin switch
    {
        "player" => CommandOriginType.Player,
        "commandblock" => CommandOriginType.CommandBlock,
        "minecartcommandblock" => CommandOriginType.MinecartCommandBlock,
        "devconsole" => CommandOriginType.DevConsole,
        "test" => CommandOriginType.Test,
        "automationplayer" => CommandOriginType.AutomationPlayer,
        "clientautomation" => CommandOriginType.ClientAutomation,
        "dedicatedserver" => CommandOriginType.DedicatedServer,
        "entity" => CommandOriginType.Entity,
        "virtual" => CommandOriginType.Virtual,
        "gameargument" => CommandOriginType.GameArgument,
        "entityserver" => CommandOriginType.EntityServer,
        "precompiled" => CommandOriginType.Precompiled,
        "gamedirectorentityserver" => CommandOriginType.GameDirectorEntityServer,
        "scripting" => CommandOriginType.Script,
        "executecontext" => CommandOriginType.Executor,
        _ => throw new InvalidOperationException($"Unknown command origin type: {origin}.")
    };
}
