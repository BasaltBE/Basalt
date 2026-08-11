using System;

namespace BedrockProtocol.Enums;

public enum ArmorSlot {
    Head = 0,
    Torso = 1,
    Legs = 2,
    Feet = 3,
    Body = 4,
}

public static class ArmorSlotExtensions {
    public static string ToProtoString(this ArmorSlot value) => value.ToProtocolString();

    public static string ToProtocolString(this ArmorSlot value) {
        return value switch {
            ArmorSlot.Head => "Head",
            ArmorSlot.Torso => "Torso",
            ArmorSlot.Legs => "Legs",
            ArmorSlot.Feet => "Feet",
            ArmorSlot.Body => "Body",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ArmorSlot value.")
        };
    }

    public static ArmorSlot FromProtocolString(string value) {
        return value switch {
            "Head" => ArmorSlot.Head,
            "Torso" => ArmorSlot.Torso,
            "Legs" => ArmorSlot.Legs,
            "Feet" => ArmorSlot.Feet,
            "Body" => ArmorSlot.Body,
            _ => throw new ArgumentException($"Unknown ArmorSlot protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ArmorSlot result) {
        switch (value) {
            case "Head":
                result = ArmorSlot.Head;
                return true;
            case "Torso":
                result = ArmorSlot.Torso;
                return true;
            case "Legs":
                result = ArmorSlot.Legs;
                return true;
            case "Feet":
                result = ArmorSlot.Feet;
                return true;
            case "Body":
                result = ArmorSlot.Body;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
