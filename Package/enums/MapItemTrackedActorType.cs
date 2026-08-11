using System;

namespace BedrockProtocol.Enums;

public enum MapItemTrackedActorType {
    Entity = 0,
    BlockEntity = 1,
    Other = 2,
}

public static class MapItemTrackedActorTypeExtensions {
    public static string ToProtoString(this MapItemTrackedActorType value) => value.ToProtocolString();

    public static string ToProtocolString(this MapItemTrackedActorType value) {
        return value switch {
            MapItemTrackedActorType.Entity => "Entity",
            MapItemTrackedActorType.BlockEntity => "BlockEntity",
            MapItemTrackedActorType.Other => "Other",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown MapItemTrackedActorType value.")
        };
    }

    public static MapItemTrackedActorType FromProtocolString(string value) {
        return value switch {
            "Entity" => MapItemTrackedActorType.Entity,
            "BlockEntity" => MapItemTrackedActorType.BlockEntity,
            "Other" => MapItemTrackedActorType.Other,
            _ => throw new ArgumentException($"Unknown MapItemTrackedActorType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out MapItemTrackedActorType result) {
        switch (value) {
            case "Entity":
                result = MapItemTrackedActorType.Entity;
                return true;
            case "BlockEntity":
                result = MapItemTrackedActorType.BlockEntity;
                return true;
            case "Other":
                result = MapItemTrackedActorType.Other;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
