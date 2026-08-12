#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum CommandOriginType {
    Player = 0,
    CommandBlock = 1,
    MinecartCommandBlock = 2,
    DevConsole = 3,
    Test = 4,
    AutomationPlayer = 5,
    ClientAutomation = 6,
    DedicatedServer = 7,
    Entity = 8,
    Virtual = 9,
    GameArgument = 10,
    EntityServer = 11,
    Precompiled = 12,
    GameDirectorEntityServer = 13,
    Scripting = 14,
    ExecuteContext = 15,
}

public static class CommandOriginTypeExtensions {
    public static string ToProtoString(this CommandOriginType value) => value.ToProtocolString();

    public static string ToProtocolString(this CommandOriginType value) {
        return value switch {
            CommandOriginType.Player => "Player",
            CommandOriginType.CommandBlock => "CommandBlock",
            CommandOriginType.MinecartCommandBlock => "MinecartCommandBlock",
            CommandOriginType.DevConsole => "DevConsole",
            CommandOriginType.Test => "Test",
            CommandOriginType.AutomationPlayer => "AutomationPlayer",
            CommandOriginType.ClientAutomation => "ClientAutomation",
            CommandOriginType.DedicatedServer => "DedicatedServer",
            CommandOriginType.Entity => "Entity",
            CommandOriginType.Virtual => "Virtual",
            CommandOriginType.GameArgument => "GameArgument",
            CommandOriginType.EntityServer => "EntityServer",
            CommandOriginType.Precompiled => "Precompiled",
            CommandOriginType.GameDirectorEntityServer => "GameDirectorEntityServer",
            CommandOriginType.Scripting => "Scripting",
            CommandOriginType.ExecuteContext => "ExecuteContext",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown CommandOriginType value.")
        };
    }

    public static CommandOriginType FromProtocolString(string value) {
        return value switch {
            "Player" => CommandOriginType.Player,
            "CommandBlock" => CommandOriginType.CommandBlock,
            "MinecartCommandBlock" => CommandOriginType.MinecartCommandBlock,
            "DevConsole" => CommandOriginType.DevConsole,
            "Test" => CommandOriginType.Test,
            "AutomationPlayer" => CommandOriginType.AutomationPlayer,
            "ClientAutomation" => CommandOriginType.ClientAutomation,
            "DedicatedServer" => CommandOriginType.DedicatedServer,
            "Entity" => CommandOriginType.Entity,
            "Virtual" => CommandOriginType.Virtual,
            "GameArgument" => CommandOriginType.GameArgument,
            "EntityServer" => CommandOriginType.EntityServer,
            "Precompiled" => CommandOriginType.Precompiled,
            "GameDirectorEntityServer" => CommandOriginType.GameDirectorEntityServer,
            "Scripting" => CommandOriginType.Scripting,
            "ExecuteContext" => CommandOriginType.ExecuteContext,
            _ => throw new ArgumentException($"Unknown CommandOriginType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out CommandOriginType result) {
        switch (value) {
            case "Player":
                result = CommandOriginType.Player;
                return true;
            case "CommandBlock":
                result = CommandOriginType.CommandBlock;
                return true;
            case "MinecartCommandBlock":
                result = CommandOriginType.MinecartCommandBlock;
                return true;
            case "DevConsole":
                result = CommandOriginType.DevConsole;
                return true;
            case "Test":
                result = CommandOriginType.Test;
                return true;
            case "AutomationPlayer":
                result = CommandOriginType.AutomationPlayer;
                return true;
            case "ClientAutomation":
                result = CommandOriginType.ClientAutomation;
                return true;
            case "DedicatedServer":
                result = CommandOriginType.DedicatedServer;
                return true;
            case "Entity":
                result = CommandOriginType.Entity;
                return true;
            case "Virtual":
                result = CommandOriginType.Virtual;
                return true;
            case "GameArgument":
                result = CommandOriginType.GameArgument;
                return true;
            case "EntityServer":
                result = CommandOriginType.EntityServer;
                return true;
            case "Precompiled":
                result = CommandOriginType.Precompiled;
                return true;
            case "GameDirectorEntityServer":
                result = CommandOriginType.GameDirectorEntityServer;
                return true;
            case "Scripting":
                result = CommandOriginType.Scripting;
                return true;
            case "ExecuteContext":
                result = CommandOriginType.ExecuteContext;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
