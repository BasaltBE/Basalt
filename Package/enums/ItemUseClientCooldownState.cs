#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum ItemUseClientCooldownState {
    Off = 0,
    On = 1,
}

public static class ItemUseClientCooldownStateExtensions {
    public static string ToProtoString(this ItemUseClientCooldownState value) => value.ToProtocolString();

    public static string ToProtocolString(this ItemUseClientCooldownState value) {
        return value switch {
            ItemUseClientCooldownState.Off => "Off",
            ItemUseClientCooldownState.On => "On",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ItemUseClientCooldownState value.")
        };
    }

    public static ItemUseClientCooldownState FromProtocolString(string value) {
        return value switch {
            "Off" => ItemUseClientCooldownState.Off,
            "On" => ItemUseClientCooldownState.On,
            _ => throw new ArgumentException($"Unknown ItemUseClientCooldownState protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ItemUseClientCooldownState result) {
        switch (value) {
            case "Off":
                result = ItemUseClientCooldownState.Off;
                return true;
            case "On":
                result = ItemUseClientCooldownState.On;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
