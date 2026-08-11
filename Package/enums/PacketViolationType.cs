using System;

namespace BedrockProtocol.Enums;

public enum PacketViolationType {
    Unknown = -1,
    PacketMalformed = 0,
}

public static class PacketViolationTypeExtensions {
    public static string ToProtoString(this PacketViolationType value) => value.ToProtocolString();

    public static string ToProtocolString(this PacketViolationType value) {
        return value switch {
            PacketViolationType.Unknown => "Unknown",
            PacketViolationType.PacketMalformed => "PacketMalformed",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PacketViolationType value.")
        };
    }

    public static PacketViolationType FromProtocolString(string value) {
        return value switch {
            "Unknown" => PacketViolationType.Unknown,
            "PacketMalformed" => PacketViolationType.PacketMalformed,
            _ => throw new ArgumentException($"Unknown PacketViolationType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PacketViolationType result) {
        switch (value) {
            case "Unknown":
                result = PacketViolationType.Unknown;
                return true;
            case "PacketMalformed":
                result = PacketViolationType.PacketMalformed;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
