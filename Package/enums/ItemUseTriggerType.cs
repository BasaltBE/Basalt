using System;

namespace BedrockProtocol.Enums;

public enum ItemUseTriggerType {
    Unknown = 0,
    PlayerInput = 1,
    SimulationTick = 2,
}

public static class ItemUseTriggerTypeExtensions {
    public static string ToProtoString(this ItemUseTriggerType value) => value.ToProtocolString();

    public static string ToProtocolString(this ItemUseTriggerType value) {
        return value switch {
            ItemUseTriggerType.Unknown => "Unknown",
            ItemUseTriggerType.PlayerInput => "Player Input",
            ItemUseTriggerType.SimulationTick => "Simulation Tick",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ItemUseTriggerType value.")
        };
    }

    public static ItemUseTriggerType FromProtocolString(string value) {
        return value switch {
            "Unknown" => ItemUseTriggerType.Unknown,
            "Player Input" => ItemUseTriggerType.PlayerInput,
            "Simulation Tick" => ItemUseTriggerType.SimulationTick,
            _ => throw new ArgumentException($"Unknown ItemUseTriggerType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ItemUseTriggerType result) {
        switch (value) {
            case "Unknown":
                result = ItemUseTriggerType.Unknown;
                return true;
            case "Player Input":
                result = ItemUseTriggerType.PlayerInput;
                return true;
            case "Simulation Tick":
                result = ItemUseTriggerType.SimulationTick;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
