using System;

namespace BedrockProtocol.Enums;

public enum EditorWorldType {
    NonEditor = 0,
    EditorProject = 1,
    EditorTestLevel = 2,
    EditorRealmsUpload = 3,
}

public static class EditorWorldTypeExtensions {
    public static string ToProtoString(this EditorWorldType value) => value.ToProtocolString();

    public static string ToProtocolString(this EditorWorldType value) {
        return value switch {
            EditorWorldType.NonEditor => "NonEditor",
            EditorWorldType.EditorProject => "EditorProject",
            EditorWorldType.EditorTestLevel => "EditorTestLevel",
            EditorWorldType.EditorRealmsUpload => "EditorRealmsUpload",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown EditorWorldType value.")
        };
    }

    public static EditorWorldType FromProtocolString(string value) {
        return value switch {
            "NonEditor" => EditorWorldType.NonEditor,
            "EditorProject" => EditorWorldType.EditorProject,
            "EditorTestLevel" => EditorWorldType.EditorTestLevel,
            "EditorRealmsUpload" => EditorWorldType.EditorRealmsUpload,
            _ => throw new ArgumentException($"Unknown EditorWorldType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out EditorWorldType result) {
        switch (value) {
            case "NonEditor":
                result = EditorWorldType.NonEditor;
                return true;
            case "EditorProject":
                result = EditorWorldType.EditorProject;
                return true;
            case "EditorTestLevel":
                result = EditorWorldType.EditorTestLevel;
                return true;
            case "EditorRealmsUpload":
                result = EditorWorldType.EditorRealmsUpload;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
