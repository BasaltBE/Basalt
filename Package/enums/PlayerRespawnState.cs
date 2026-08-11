using System;

namespace BedrockProtocol.Enums;

public enum PlayerRespawnState {
    SearchingForSpawn = 0,
    ReadyToSpawn = 1,
    ClientReadyToSpawn = 2,
}

public static class PlayerRespawnStateExtensions {
    public static string ToProtoString(this PlayerRespawnState value) => value.ToProtocolString();

    public static string ToProtocolString(this PlayerRespawnState value) {
        return value switch {
            PlayerRespawnState.SearchingForSpawn => "SearchingForSpawn",
            PlayerRespawnState.ReadyToSpawn => "ReadyToSpawn",
            PlayerRespawnState.ClientReadyToSpawn => "ClientReadyToSpawn",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PlayerRespawnState value.")
        };
    }

    public static PlayerRespawnState FromProtocolString(string value) {
        return value switch {
            "SearchingForSpawn" => PlayerRespawnState.SearchingForSpawn,
            "ReadyToSpawn" => PlayerRespawnState.ReadyToSpawn,
            "ClientReadyToSpawn" => PlayerRespawnState.ClientReadyToSpawn,
            _ => throw new ArgumentException($"Unknown PlayerRespawnState protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PlayerRespawnState result) {
        switch (value) {
            case "SearchingForSpawn":
                result = PlayerRespawnState.SearchingForSpawn;
                return true;
            case "ReadyToSpawn":
                result = PlayerRespawnState.ReadyToSpawn;
                return true;
            case "ClientReadyToSpawn":
                result = PlayerRespawnState.ClientReadyToSpawn;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
