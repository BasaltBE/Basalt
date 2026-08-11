using System;

namespace BedrockProtocol.Enums;

public enum ServerEditorConnectionPolicy {
    MatchWorldType = 0,
    EditorOnly = 1,
    VanillaOnly = 2,
    Mixed = 3,
}

public static class ServerEditorConnectionPolicyExtensions {
    public static string ToProtoString(this ServerEditorConnectionPolicy value) => value.ToProtocolString();

    public static string ToProtocolString(this ServerEditorConnectionPolicy value) {
        return value switch {
            ServerEditorConnectionPolicy.MatchWorldType => "MatchWorldType",
            ServerEditorConnectionPolicy.EditorOnly => "EditorOnly",
            ServerEditorConnectionPolicy.VanillaOnly => "VanillaOnly",
            ServerEditorConnectionPolicy.Mixed => "Mixed",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ServerEditorConnectionPolicy value.")
        };
    }

    public static ServerEditorConnectionPolicy FromProtocolString(string value) {
        return value switch {
            "MatchWorldType" => ServerEditorConnectionPolicy.MatchWorldType,
            "EditorOnly" => ServerEditorConnectionPolicy.EditorOnly,
            "VanillaOnly" => ServerEditorConnectionPolicy.VanillaOnly,
            "Mixed" => ServerEditorConnectionPolicy.Mixed,
            _ => throw new ArgumentException($"Unknown ServerEditorConnectionPolicy protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ServerEditorConnectionPolicy result) {
        switch (value) {
            case "MatchWorldType":
                result = ServerEditorConnectionPolicy.MatchWorldType;
                return true;
            case "EditorOnly":
                result = ServerEditorConnectionPolicy.EditorOnly;
                return true;
            case "VanillaOnly":
                result = ServerEditorConnectionPolicy.VanillaOnly;
                return true;
            case "Mixed":
                result = ServerEditorConnectionPolicy.Mixed;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
