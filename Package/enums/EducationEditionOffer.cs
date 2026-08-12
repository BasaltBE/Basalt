#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum EducationEditionOffer {
    None = 0,
    RestOfWorld = 1,
    China_Deprecated = 2,
}

public static class EducationEditionOfferExtensions {
    public static string ToProtoString(this EducationEditionOffer value) => value.ToProtocolString();

    public static string ToProtocolString(this EducationEditionOffer value) {
        return value switch {
            EducationEditionOffer.None => "None",
            EducationEditionOffer.RestOfWorld => "RestOfWorld",
            EducationEditionOffer.China_Deprecated => "China_Deprecated",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown EducationEditionOffer value.")
        };
    }

    public static EducationEditionOffer FromProtocolString(string value) {
        return value switch {
            "None" => EducationEditionOffer.None,
            "RestOfWorld" => EducationEditionOffer.RestOfWorld,
            "China_Deprecated" => EducationEditionOffer.China_Deprecated,
            _ => throw new ArgumentException($"Unknown EducationEditionOffer protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out EducationEditionOffer result) {
        switch (value) {
            case "None":
                result = EducationEditionOffer.None;
                return true;
            case "RestOfWorld":
                result = EducationEditionOffer.RestOfWorld;
                return true;
            case "China_Deprecated":
                result = EducationEditionOffer.China_Deprecated;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
