#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum VillageType {
    Desert = 0,
    Ice = 1,
    Savanna = 2,
    Taiga = 3,
    Default = 4,
}

public static class VillageTypeExtensions {
    public static string ToProtoString(this VillageType value) => value.ToProtocolString();

    public static string ToProtocolString(this VillageType value) {
        return value switch {
            VillageType.Desert => "Desert",
            VillageType.Ice => "Ice",
            VillageType.Savanna => "Savanna",
            VillageType.Taiga => "Taiga",
            VillageType.Default => "Default",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown VillageType value.")
        };
    }

    public static VillageType FromProtocolString(string value) {
        return value switch {
            "Desert" => VillageType.Desert,
            "Ice" => VillageType.Ice,
            "Savanna" => VillageType.Savanna,
            "Taiga" => VillageType.Taiga,
            "Default" => VillageType.Default,
            _ => throw new ArgumentException($"Unknown VillageType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out VillageType result) {
        switch (value) {
            case "Desert":
                result = VillageType.Desert;
                return true;
            case "Ice":
                result = VillageType.Ice;
                return true;
            case "Savanna":
                result = VillageType.Savanna;
                return true;
            case "Taiga":
                result = VillageType.Taiga;
                return true;
            case "Default":
                result = VillageType.Default;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
