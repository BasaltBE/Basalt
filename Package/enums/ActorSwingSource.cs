using System;

namespace BedrockProtocol.Enums;

public enum ActorSwingSource {
    None = 0,
    Build = 1,
    Mine = 2,
    Interact = 3,
    Attack = 4,
    UseItem = 5,
    ThrowItem = 6,
    DropItem = 7,
    Event = 8,
}

public static class ActorSwingSourceExtensions {
    public static string ToProtoString(this ActorSwingSource value) => value.ToProtocolString();

    public static string ToProtocolString(this ActorSwingSource value) {
        return value switch {
            ActorSwingSource.None => "None",
            ActorSwingSource.Build => "Build",
            ActorSwingSource.Mine => "Mine",
            ActorSwingSource.Interact => "Interact",
            ActorSwingSource.Attack => "Attack",
            ActorSwingSource.UseItem => "UseItem",
            ActorSwingSource.ThrowItem => "ThrowItem",
            ActorSwingSource.DropItem => "DropItem",
            ActorSwingSource.Event => "Event",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ActorSwingSource value.")
        };
    }

    public static ActorSwingSource FromProtocolString(string value) {
        return value switch {
            "None" => ActorSwingSource.None,
            "Build" => ActorSwingSource.Build,
            "Mine" => ActorSwingSource.Mine,
            "Interact" => ActorSwingSource.Interact,
            "Attack" => ActorSwingSource.Attack,
            "UseItem" => ActorSwingSource.UseItem,
            "ThrowItem" => ActorSwingSource.ThrowItem,
            "DropItem" => ActorSwingSource.DropItem,
            "Event" => ActorSwingSource.Event,
            _ => throw new ArgumentException($"Unknown ActorSwingSource protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ActorSwingSource result) {
        switch (value) {
            case "None":
                result = ActorSwingSource.None;
                return true;
            case "Build":
                result = ActorSwingSource.Build;
                return true;
            case "Mine":
                result = ActorSwingSource.Mine;
                return true;
            case "Interact":
                result = ActorSwingSource.Interact;
                return true;
            case "Attack":
                result = ActorSwingSource.Attack;
                return true;
            case "UseItem":
                result = ActorSwingSource.UseItem;
                return true;
            case "ThrowItem":
                result = ActorSwingSource.ThrowItem;
                return true;
            case "DropItem":
                result = ActorSwingSource.DropItem;
                return true;
            case "Event":
                result = ActorSwingSource.Event;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
