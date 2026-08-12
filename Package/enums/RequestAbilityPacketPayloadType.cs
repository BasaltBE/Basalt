#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum RequestAbilityPacketPayloadType {
    Unset = 0,
    Bool = 1,
    Float = 2,
}

public static class RequestAbilityPacketPayloadTypeExtensions {
    public static string ToProtoString(this RequestAbilityPacketPayloadType value) => value.ToProtocolString();

    public static string ToProtocolString(this RequestAbilityPacketPayloadType value) {
        return value switch {
            RequestAbilityPacketPayloadType.Unset => "Unset",
            RequestAbilityPacketPayloadType.Bool => "Bool",
            RequestAbilityPacketPayloadType.Float => "Float",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown RequestAbilityPacketPayloadType value.")
        };
    }

    public static RequestAbilityPacketPayloadType FromProtocolString(string value) {
        return value switch {
            "Unset" => RequestAbilityPacketPayloadType.Unset,
            "Bool" => RequestAbilityPacketPayloadType.Bool,
            "Float" => RequestAbilityPacketPayloadType.Float,
            _ => throw new ArgumentException($"Unknown RequestAbilityPacketPayloadType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out RequestAbilityPacketPayloadType result) {
        switch (value) {
            case "Unset":
                result = RequestAbilityPacketPayloadType.Unset;
                return true;
            case "Bool":
                result = RequestAbilityPacketPayloadType.Bool;
                return true;
            case "Float":
                result = RequestAbilityPacketPayloadType.Float;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
