#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum PacketViolationSeverity {
    Unknown = -1,
    Warning = 0,
    FinalWarning = 1,
    TerminatingConnection = 2,
}

public static class PacketViolationSeverityExtensions {
    public static string ToProtoString(this PacketViolationSeverity value) => value.ToProtocolString();

    public static string ToProtocolString(this PacketViolationSeverity value) {
        return value switch {
            PacketViolationSeverity.Unknown => "Unknown",
            PacketViolationSeverity.Warning => "Warning",
            PacketViolationSeverity.FinalWarning => "FinalWarning",
            PacketViolationSeverity.TerminatingConnection => "TerminatingConnection",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PacketViolationSeverity value.")
        };
    }

    public static PacketViolationSeverity FromProtocolString(string value) {
        return value switch {
            "Unknown" => PacketViolationSeverity.Unknown,
            "Warning" => PacketViolationSeverity.Warning,
            "FinalWarning" => PacketViolationSeverity.FinalWarning,
            "TerminatingConnection" => PacketViolationSeverity.TerminatingConnection,
            _ => throw new ArgumentException($"Unknown PacketViolationSeverity protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PacketViolationSeverity result) {
        switch (value) {
            case "Unknown":
                result = PacketViolationSeverity.Unknown;
                return true;
            case "Warning":
                result = PacketViolationSeverity.Warning;
                return true;
            case "FinalWarning":
                result = PacketViolationSeverity.FinalWarning;
                return true;
            case "TerminatingConnection":
                result = PacketViolationSeverity.TerminatingConnection;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
