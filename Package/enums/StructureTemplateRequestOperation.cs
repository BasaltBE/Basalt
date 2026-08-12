#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum StructureTemplateRequestOperation {
    None = 0,
    ExportFromSaveMode = 1,
    ExportFromLoadMode = 2,
    QuerySavedStructure = 3,
}

public static class StructureTemplateRequestOperationExtensions {
    public static string ToProtoString(this StructureTemplateRequestOperation value) => value.ToProtocolString();

    public static string ToProtocolString(this StructureTemplateRequestOperation value) {
        return value switch {
            StructureTemplateRequestOperation.None => "None",
            StructureTemplateRequestOperation.ExportFromSaveMode => "ExportFromSaveMode",
            StructureTemplateRequestOperation.ExportFromLoadMode => "ExportFromLoadMode",
            StructureTemplateRequestOperation.QuerySavedStructure => "QuerySavedStructure",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown StructureTemplateRequestOperation value.")
        };
    }

    public static StructureTemplateRequestOperation FromProtocolString(string value) {
        return value switch {
            "None" => StructureTemplateRequestOperation.None,
            "ExportFromSaveMode" => StructureTemplateRequestOperation.ExportFromSaveMode,
            "ExportFromLoadMode" => StructureTemplateRequestOperation.ExportFromLoadMode,
            "QuerySavedStructure" => StructureTemplateRequestOperation.QuerySavedStructure,
            _ => throw new ArgumentException($"Unknown StructureTemplateRequestOperation protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out StructureTemplateRequestOperation result) {
        switch (value) {
            case "None":
                result = StructureTemplateRequestOperation.None;
                return true;
            case "ExportFromSaveMode":
                result = StructureTemplateRequestOperation.ExportFromSaveMode;
                return true;
            case "ExportFromLoadMode":
                result = StructureTemplateRequestOperation.ExportFromLoadMode;
                return true;
            case "QuerySavedStructure":
                result = StructureTemplateRequestOperation.QuerySavedStructure;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
