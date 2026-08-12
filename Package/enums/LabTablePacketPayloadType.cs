#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum LabTablePacketPayloadType {
    StartCombine = 0,
    StartReaction = 1,
    Reset = 2,
}

public static class LabTablePacketPayloadTypeExtensions {
    public static string ToProtoString(this LabTablePacketPayloadType value) => value.ToProtocolString();

    public static string ToProtocolString(this LabTablePacketPayloadType value) {
        return value switch {
            LabTablePacketPayloadType.StartCombine => "StartCombine",
            LabTablePacketPayloadType.StartReaction => "StartReaction",
            LabTablePacketPayloadType.Reset => "Reset",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown LabTablePacketPayloadType value.")
        };
    }

    public static LabTablePacketPayloadType FromProtocolString(string value) {
        return value switch {
            "StartCombine" => LabTablePacketPayloadType.StartCombine,
            "StartReaction" => LabTablePacketPayloadType.StartReaction,
            "Reset" => LabTablePacketPayloadType.Reset,
            _ => throw new ArgumentException($"Unknown LabTablePacketPayloadType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out LabTablePacketPayloadType result) {
        switch (value) {
            case "StartCombine":
                result = LabTablePacketPayloadType.StartCombine;
                return true;
            case "StartReaction":
                result = LabTablePacketPayloadType.StartReaction;
                return true;
            case "Reset":
                result = LabTablePacketPayloadType.Reset;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
