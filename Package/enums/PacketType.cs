using System;

namespace BedrockProtocol.Enums;

public enum PacketType {
    Empty = 0,
    InitiallyUnlockedRecipes = 1,
    NewlyUnlockedRecipes = 2,
    RemoveUnlockedRecipes = 3,
    RemoveAllUnlockedRecipes = 4,
}

public static class PacketTypeExtensions {
    public static string ToProtoString(this PacketType value) => value.ToProtocolString();

    public static string ToProtocolString(this PacketType value) {
        return value switch {
            PacketType.Empty => "Empty",
            PacketType.InitiallyUnlockedRecipes => "InitiallyUnlockedRecipes",
            PacketType.NewlyUnlockedRecipes => "NewlyUnlockedRecipes",
            PacketType.RemoveUnlockedRecipes => "RemoveUnlockedRecipes",
            PacketType.RemoveAllUnlockedRecipes => "RemoveAllUnlockedRecipes",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PacketType value.")
        };
    }

    public static PacketType FromProtocolString(string value) {
        return value switch {
            "Empty" => PacketType.Empty,
            "InitiallyUnlockedRecipes" => PacketType.InitiallyUnlockedRecipes,
            "NewlyUnlockedRecipes" => PacketType.NewlyUnlockedRecipes,
            "RemoveUnlockedRecipes" => PacketType.RemoveUnlockedRecipes,
            "RemoveAllUnlockedRecipes" => PacketType.RemoveAllUnlockedRecipes,
            _ => throw new ArgumentException($"Unknown PacketType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PacketType result) {
        switch (value) {
            case "Empty":
                result = PacketType.Empty;
                return true;
            case "InitiallyUnlockedRecipes":
                result = PacketType.InitiallyUnlockedRecipes;
                return true;
            case "NewlyUnlockedRecipes":
                result = PacketType.NewlyUnlockedRecipes;
                return true;
            case "RemoveUnlockedRecipes":
                result = PacketType.RemoveUnlockedRecipes;
                return true;
            case "RemoveAllUnlockedRecipes":
                result = PacketType.RemoveAllUnlockedRecipes;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
