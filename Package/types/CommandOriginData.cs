using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CommandOriginData {
    public global::BedrockProtocol.Enums.CommandOriginType Type;
    public UUID UUID = new();
    public string RequestId = string.Empty;
    public long PlayerId;

    public void Read(BinaryReader reader) {
        string enumText0 = reader.ReadVarString();
        Type = enumText0 switch {
            "player" => global::BedrockProtocol.Enums.CommandOriginType.Player,
            "commandblock" => global::BedrockProtocol.Enums.CommandOriginType.CommandBlock,
            "minecartcommandblock" => global::BedrockProtocol.Enums.CommandOriginType.MinecartCommandBlock,
            "devconsole" => global::BedrockProtocol.Enums.CommandOriginType.DevConsole,
            "test" => global::BedrockProtocol.Enums.CommandOriginType.Test,
            "automationplayer" => global::BedrockProtocol.Enums.CommandOriginType.AutomationPlayer,
            "clientautomation" => global::BedrockProtocol.Enums.CommandOriginType.ClientAutomation,
            "dedicatedserver" => global::BedrockProtocol.Enums.CommandOriginType.DedicatedServer,
            "entity" => global::BedrockProtocol.Enums.CommandOriginType.Entity,
            "virtual" => global::BedrockProtocol.Enums.CommandOriginType.Virtual,
            "gameargument" => global::BedrockProtocol.Enums.CommandOriginType.GameArgument,
            "entityserver" => global::BedrockProtocol.Enums.CommandOriginType.EntityServer,
            "precompiled" => global::BedrockProtocol.Enums.CommandOriginType.Precompiled,
            "gamedirectorentityserver" => global::BedrockProtocol.Enums.CommandOriginType.GameDirectorEntityServer,
            "scripting" => global::BedrockProtocol.Enums.CommandOriginType.Scripting,
            "executecontext" => global::BedrockProtocol.Enums.CommandOriginType.ExecuteContext,
            _ => throw new InvalidOperationException($"Unknown CommandOriginType wire value: {enumText0}"),
        };
        UUID.Read(reader);
        RequestId = reader.ReadVarString();
        PlayerId = reader.ReadInt64(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Type switch {
            global::BedrockProtocol.Enums.CommandOriginType.Player => "player",
            global::BedrockProtocol.Enums.CommandOriginType.CommandBlock => "commandblock",
            global::BedrockProtocol.Enums.CommandOriginType.MinecartCommandBlock => "minecartcommandblock",
            global::BedrockProtocol.Enums.CommandOriginType.DevConsole => "devconsole",
            global::BedrockProtocol.Enums.CommandOriginType.Test => "test",
            global::BedrockProtocol.Enums.CommandOriginType.AutomationPlayer => "automationplayer",
            global::BedrockProtocol.Enums.CommandOriginType.ClientAutomation => "clientautomation",
            global::BedrockProtocol.Enums.CommandOriginType.DedicatedServer => "dedicatedserver",
            global::BedrockProtocol.Enums.CommandOriginType.Entity => "entity",
            global::BedrockProtocol.Enums.CommandOriginType.Virtual => "virtual",
            global::BedrockProtocol.Enums.CommandOriginType.GameArgument => "gameargument",
            global::BedrockProtocol.Enums.CommandOriginType.EntityServer => "entityserver",
            global::BedrockProtocol.Enums.CommandOriginType.Precompiled => "precompiled",
            global::BedrockProtocol.Enums.CommandOriginType.GameDirectorEntityServer => "gamedirectorentityserver",
            global::BedrockProtocol.Enums.CommandOriginType.Scripting => "scripting",
            global::BedrockProtocol.Enums.CommandOriginType.ExecuteContext => "executecontext",
            _ => throw new InvalidOperationException($"Unknown CommandOriginType value: {Type}"),
        });
        UUID.Write(writer);
        writer.WriteVarString(RequestId);
        writer.WriteInt64(PlayerId, true);
    }
}
