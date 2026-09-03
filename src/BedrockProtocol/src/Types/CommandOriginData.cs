using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class CommandOriginData : DataType {
    public CommandOriginType Type;
    public Uuid Uuid = new();
    public string RequestId = string.Empty;
    public long PlayerId = -1;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(TypeToString(Type));
        Uuid.Write(ref writer);
        writer.WriteVarString(RequestId);
        writer.WriteInt64(PlayerId, true);
    }

    public override void Read(ref BinaryReader reader) {
        Type = StringToType(reader.ReadVarString());
        Uuid.Read(ref reader);
        RequestId = reader.ReadVarString();
        PlayerId = reader.ReadInt64(true);
    }

    private static string TypeToString(CommandOriginType type) => type switch {
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
        CommandOriginType.Scripting => "scripting",
        CommandOriginType.ExecuteContext => "executecontext",
        _ => throw new InvalidOperationException($"Unknown command origin type: {type}.")
    };

    private static CommandOriginType StringToType(string type) => type switch {
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
        "scripting" => CommandOriginType.Scripting,
        "executecontext" => CommandOriginType.ExecuteContext,
        _ => throw new InvalidOperationException($"Unknown command origin type: {type}.")
    };
}
