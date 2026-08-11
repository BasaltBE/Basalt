using System;

namespace BedrockProtocol.Enums;

public enum PacketCompressionAlgorithm {
    ZLib = 0,
    Snappy = 1,
    None = 65535,
}

public static class PacketCompressionAlgorithmExtensions {
    public static string ToProtoString(this PacketCompressionAlgorithm value) => value.ToProtocolString();

    public static string ToProtocolString(this PacketCompressionAlgorithm value) {
        return value switch {
            PacketCompressionAlgorithm.ZLib => "ZLib",
            PacketCompressionAlgorithm.Snappy => "Snappy",
            PacketCompressionAlgorithm.None => "None",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PacketCompressionAlgorithm value.")
        };
    }

    public static PacketCompressionAlgorithm FromProtocolString(string value) {
        return value switch {
            "ZLib" => PacketCompressionAlgorithm.ZLib,
            "Snappy" => PacketCompressionAlgorithm.Snappy,
            "None" => PacketCompressionAlgorithm.None,
            _ => throw new ArgumentException($"Unknown PacketCompressionAlgorithm protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PacketCompressionAlgorithm result) {
        switch (value) {
            case "ZLib":
                result = PacketCompressionAlgorithm.ZLib;
                return true;
            case "Snappy":
                result = PacketCompressionAlgorithm.Snappy;
                return true;
            case "None":
                result = PacketCompressionAlgorithm.None;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
