using System;

namespace BedrockProtocol.Enums;

public enum InteractionType {
    Breeding = 1,
    Taming = 2,
    Curing = 3,
    Crafted = 4,
    Shearing = 5,
    Milking = 6,
    Trading = 7,
    Feeding = 8,
    Igniting = 9,
    Coloring = 10,
    Naming = 11,
    Leashing = 12,
    Unleashing = 13,
    PetSleep = 14,
    Trusting = 15,
    Commanding = 16,
    Equipping = 17,
}

public static class InteractionTypeExtensions {
    public static string ToProtoString(this InteractionType value) => value.ToProtocolString();

    public static string ToProtocolString(this InteractionType value) {
        return value switch {
            InteractionType.Breeding => "Breeding",
            InteractionType.Taming => "Taming",
            InteractionType.Curing => "Curing",
            InteractionType.Crafted => "Crafted",
            InteractionType.Shearing => "Shearing",
            InteractionType.Milking => "Milking",
            InteractionType.Trading => "Trading",
            InteractionType.Feeding => "Feeding",
            InteractionType.Igniting => "Igniting",
            InteractionType.Coloring => "Coloring",
            InteractionType.Naming => "Naming",
            InteractionType.Leashing => "Leashing",
            InteractionType.Unleashing => "Unleashing",
            InteractionType.PetSleep => "PetSleep",
            InteractionType.Trusting => "Trusting",
            InteractionType.Commanding => "Commanding",
            InteractionType.Equipping => "Equipping",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown InteractionType value.")
        };
    }

    public static InteractionType FromProtocolString(string value) {
        return value switch {
            "Breeding" => InteractionType.Breeding,
            "Taming" => InteractionType.Taming,
            "Curing" => InteractionType.Curing,
            "Crafted" => InteractionType.Crafted,
            "Shearing" => InteractionType.Shearing,
            "Milking" => InteractionType.Milking,
            "Trading" => InteractionType.Trading,
            "Feeding" => InteractionType.Feeding,
            "Igniting" => InteractionType.Igniting,
            "Coloring" => InteractionType.Coloring,
            "Naming" => InteractionType.Naming,
            "Leashing" => InteractionType.Leashing,
            "Unleashing" => InteractionType.Unleashing,
            "PetSleep" => InteractionType.PetSleep,
            "Trusting" => InteractionType.Trusting,
            "Commanding" => InteractionType.Commanding,
            "Equipping" => InteractionType.Equipping,
            _ => throw new ArgumentException($"Unknown InteractionType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out InteractionType result) {
        switch (value) {
            case "Breeding":
                result = InteractionType.Breeding;
                return true;
            case "Taming":
                result = InteractionType.Taming;
                return true;
            case "Curing":
                result = InteractionType.Curing;
                return true;
            case "Crafted":
                result = InteractionType.Crafted;
                return true;
            case "Shearing":
                result = InteractionType.Shearing;
                return true;
            case "Milking":
                result = InteractionType.Milking;
                return true;
            case "Trading":
                result = InteractionType.Trading;
                return true;
            case "Feeding":
                result = InteractionType.Feeding;
                return true;
            case "Igniting":
                result = InteractionType.Igniting;
                return true;
            case "Coloring":
                result = InteractionType.Coloring;
                return true;
            case "Naming":
                result = InteractionType.Naming;
                return true;
            case "Leashing":
                result = InteractionType.Leashing;
                return true;
            case "Unleashing":
                result = InteractionType.Unleashing;
                return true;
            case "PetSleep":
                result = InteractionType.PetSleep;
                return true;
            case "Trusting":
                result = InteractionType.Trusting;
                return true;
            case "Commanding":
                result = InteractionType.Commanding;
                return true;
            case "Equipping":
                result = InteractionType.Equipping;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
