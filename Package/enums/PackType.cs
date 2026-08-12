#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum PackType {
    Invalid = 0,
    Addon = 1,
    Cached = 2,
    CopyProtected = 3,
    Behavior = 4,
    PersonaPiece = 5,
    Resources = 6,
    Skins = 7,
    WorldTemplate = 8,
}

public static class PackTypeExtensions {
    public static string ToProtoString(this PackType value) => value.ToProtocolString();

    public static string ToProtocolString(this PackType value) {
        return value switch {
            PackType.Invalid => "Invalid",
            PackType.Addon => "Addon",
            PackType.Cached => "Cached",
            PackType.CopyProtected => "CopyProtected",
            PackType.Behavior => "Behavior",
            PackType.PersonaPiece => "PersonaPiece",
            PackType.Resources => "Resources",
            PackType.Skins => "Skins",
            PackType.WorldTemplate => "WorldTemplate",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PackType value.")
        };
    }

    public static PackType FromProtocolString(string value) {
        return value switch {
            "Invalid" => PackType.Invalid,
            "Addon" => PackType.Addon,
            "Cached" => PackType.Cached,
            "CopyProtected" => PackType.CopyProtected,
            "Behavior" => PackType.Behavior,
            "PersonaPiece" => PackType.PersonaPiece,
            "Resources" => PackType.Resources,
            "Skins" => PackType.Skins,
            "WorldTemplate" => PackType.WorldTemplate,
            _ => throw new ArgumentException($"Unknown PackType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PackType result) {
        switch (value) {
            case "Invalid":
                result = PackType.Invalid;
                return true;
            case "Addon":
                result = PackType.Addon;
                return true;
            case "Cached":
                result = PackType.Cached;
                return true;
            case "CopyProtected":
                result = PackType.CopyProtected;
                return true;
            case "Behavior":
                result = PackType.Behavior;
                return true;
            case "PersonaPiece":
                result = PackType.PersonaPiece;
                return true;
            case "Resources":
                result = PackType.Resources;
                return true;
            case "Skins":
                result = PackType.Skins;
                return true;
            case "WorldTemplate":
                result = PackType.WorldTemplate;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
