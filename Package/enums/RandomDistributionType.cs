using System;

namespace BedrockProtocol.Enums;

public enum RandomDistributionType {
    SingleValued = 0,
    Uniform = 1,
    Gaussian = 2,
    InverseGaussian = 3,
    FixedGrid = 4,
    JitteredGrid = 5,
    Triangle = 6,
}

public static class RandomDistributionTypeExtensions {
    public static string ToProtoString(this RandomDistributionType value) => value.ToProtocolString();

    public static string ToProtocolString(this RandomDistributionType value) {
        return value switch {
            RandomDistributionType.SingleValued => "SingleValued",
            RandomDistributionType.Uniform => "Uniform",
            RandomDistributionType.Gaussian => "Gaussian",
            RandomDistributionType.InverseGaussian => "InverseGaussian",
            RandomDistributionType.FixedGrid => "FixedGrid",
            RandomDistributionType.JitteredGrid => "JitteredGrid",
            RandomDistributionType.Triangle => "Triangle",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown RandomDistributionType value.")
        };
    }

    public static RandomDistributionType FromProtocolString(string value) {
        return value switch {
            "SingleValued" => RandomDistributionType.SingleValued,
            "Uniform" => RandomDistributionType.Uniform,
            "Gaussian" => RandomDistributionType.Gaussian,
            "InverseGaussian" => RandomDistributionType.InverseGaussian,
            "FixedGrid" => RandomDistributionType.FixedGrid,
            "JitteredGrid" => RandomDistributionType.JitteredGrid,
            "Triangle" => RandomDistributionType.Triangle,
            _ => throw new ArgumentException($"Unknown RandomDistributionType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out RandomDistributionType result) {
        switch (value) {
            case "SingleValued":
                result = RandomDistributionType.SingleValued;
                return true;
            case "Uniform":
                result = RandomDistributionType.Uniform;
                return true;
            case "Gaussian":
                result = RandomDistributionType.Gaussian;
                return true;
            case "InverseGaussian":
                result = RandomDistributionType.InverseGaussian;
                return true;
            case "FixedGrid":
                result = RandomDistributionType.FixedGrid;
                return true;
            case "JitteredGrid":
                result = RandomDistributionType.JitteredGrid;
                return true;
            case "Triangle":
                result = RandomDistributionType.Triangle;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
