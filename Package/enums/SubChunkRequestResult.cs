#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum SubChunkRequestResult {
    Undefined = 0,
    Success = 1,
    LevelChunkDoesntExist = 2,
    WrongDimension = 3,
    PlayerDoesntExist = 4,
    IndexOutOfBounds = 5,
    SuccessAllAir = 6,
}

public static class SubChunkRequestResultExtensions {
    public static string ToProtoString(this SubChunkRequestResult value) => value.ToProtocolString();

    public static string ToProtocolString(this SubChunkRequestResult value) {
        return value switch {
            SubChunkRequestResult.Undefined => "Undefined",
            SubChunkRequestResult.Success => "Success",
            SubChunkRequestResult.LevelChunkDoesntExist => "LevelChunkDoesntExist",
            SubChunkRequestResult.WrongDimension => "WrongDimension",
            SubChunkRequestResult.PlayerDoesntExist => "PlayerDoesntExist",
            SubChunkRequestResult.IndexOutOfBounds => "IndexOutOfBounds",
            SubChunkRequestResult.SuccessAllAir => "SuccessAllAir",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown SubChunkRequestResult value.")
        };
    }

    public static SubChunkRequestResult FromProtocolString(string value) {
        return value switch {
            "Undefined" => SubChunkRequestResult.Undefined,
            "Success" => SubChunkRequestResult.Success,
            "LevelChunkDoesntExist" => SubChunkRequestResult.LevelChunkDoesntExist,
            "WrongDimension" => SubChunkRequestResult.WrongDimension,
            "PlayerDoesntExist" => SubChunkRequestResult.PlayerDoesntExist,
            "IndexOutOfBounds" => SubChunkRequestResult.IndexOutOfBounds,
            "SuccessAllAir" => SubChunkRequestResult.SuccessAllAir,
            _ => throw new ArgumentException($"Unknown SubChunkRequestResult protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out SubChunkRequestResult result) {
        switch (value) {
            case "Undefined":
                result = SubChunkRequestResult.Undefined;
                return true;
            case "Success":
                result = SubChunkRequestResult.Success;
                return true;
            case "LevelChunkDoesntExist":
                result = SubChunkRequestResult.LevelChunkDoesntExist;
                return true;
            case "WrongDimension":
                result = SubChunkRequestResult.WrongDimension;
                return true;
            case "PlayerDoesntExist":
                result = SubChunkRequestResult.PlayerDoesntExist;
                return true;
            case "IndexOutOfBounds":
                result = SubChunkRequestResult.IndexOutOfBounds;
                return true;
            case "SuccessAllAir":
                result = SubChunkRequestResult.SuccessAllAir;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
