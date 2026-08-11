using System;

namespace BedrockProtocol.Enums;

public enum GeneratorType {
    Legacy = 0,
    Overworld = 1,
    Flat = 2,
    Nether = 3,
    TheEnd = 4,
    Void = 5,
    Undefined = 6,
}

public static class GeneratorTypeExtensions {
    public static string ToProtoString(this GeneratorType value) => value.ToProtocolString();

    public static string ToProtocolString(this GeneratorType value) {
        return value switch {
            GeneratorType.Legacy => "Legacy",
            GeneratorType.Overworld => "Overworld",
            GeneratorType.Flat => "Flat",
            GeneratorType.Nether => "Nether",
            GeneratorType.TheEnd => "TheEnd",
            GeneratorType.Void => "Void",
            GeneratorType.Undefined => "Undefined",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown GeneratorType value.")
        };
    }

    public static GeneratorType FromProtocolString(string value) {
        return value switch {
            "Legacy" => GeneratorType.Legacy,
            "Overworld" => GeneratorType.Overworld,
            "Flat" => GeneratorType.Flat,
            "Nether" => GeneratorType.Nether,
            "TheEnd" => GeneratorType.TheEnd,
            "Void" => GeneratorType.Void,
            "Undefined" => GeneratorType.Undefined,
            _ => throw new ArgumentException($"Unknown GeneratorType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out GeneratorType result) {
        switch (value) {
            case "Legacy":
                result = GeneratorType.Legacy;
                return true;
            case "Overworld":
                result = GeneratorType.Overworld;
                return true;
            case "Flat":
                result = GeneratorType.Flat;
                return true;
            case "Nether":
                result = GeneratorType.Nether;
                return true;
            case "TheEnd":
                result = GeneratorType.TheEnd;
                return true;
            case "Void":
                result = GeneratorType.Void;
                return true;
            case "Undefined":
                result = GeneratorType.Undefined;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
