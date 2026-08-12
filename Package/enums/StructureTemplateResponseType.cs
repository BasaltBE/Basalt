#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum StructureTemplateResponseType {
    None = 0,
    Export = 1,
    Query = 2,
}

public static class StructureTemplateResponseTypeExtensions {
    public static string ToProtoString(this StructureTemplateResponseType value) => value.ToProtocolString();

    public static string ToProtocolString(this StructureTemplateResponseType value) {
        return value switch {
            StructureTemplateResponseType.None => "None",
            StructureTemplateResponseType.Export => "Export",
            StructureTemplateResponseType.Query => "Query",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown StructureTemplateResponseType value.")
        };
    }

    public static StructureTemplateResponseType FromProtocolString(string value) {
        return value switch {
            "None" => StructureTemplateResponseType.None,
            "Export" => StructureTemplateResponseType.Export,
            "Query" => StructureTemplateResponseType.Query,
            _ => throw new ArgumentException($"Unknown StructureTemplateResponseType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out StructureTemplateResponseType result) {
        switch (value) {
            case "None":
                result = StructureTemplateResponseType.None;
                return true;
            case "Export":
                result = StructureTemplateResponseType.Export;
                return true;
            case "Query":
                result = StructureTemplateResponseType.Query;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
