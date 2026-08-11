using System;

namespace BedrockProtocol.Enums;

public enum SpawnPositionType {
    PlayerRespawn = 0,
    WorldSpawn = 1,
}

public static class SpawnPositionTypeExtensions {
    public static string ToProtoString(this SpawnPositionType value) => value.ToProtocolString();

    public static string ToProtocolString(this SpawnPositionType value) {
        return value switch {
            SpawnPositionType.PlayerRespawn => "PlayerRespawn",
            SpawnPositionType.WorldSpawn => "WorldSpawn",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown SpawnPositionType value.")
        };
    }

    public static SpawnPositionType FromProtocolString(string value) {
        return value switch {
            "PlayerRespawn" => SpawnPositionType.PlayerRespawn,
            "WorldSpawn" => SpawnPositionType.WorldSpawn,
            _ => throw new ArgumentException($"Unknown SpawnPositionType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out SpawnPositionType result) {
        switch (value) {
            case "PlayerRespawn":
                result = SpawnPositionType.PlayerRespawn;
                return true;
            case "WorldSpawn":
                result = SpawnPositionType.WorldSpawn;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
