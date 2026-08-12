#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum Event {
    Invalid = 0,
    Add = 1,
    Update = 2,
    Remove = 3,
}

public static class EventExtensions {
    public static string ToProtoString(this Event value) => value.ToProtocolString();

    public static string ToProtocolString(this Event value) {
        return value switch {
            Event.Invalid => "Invalid",
            Event.Add => "Add",
            Event.Update => "Update",
            Event.Remove => "Remove",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown Event value.")
        };
    }

    public static Event FromProtocolString(string value) {
        return value switch {
            "Invalid" => Event.Invalid,
            "Add" => Event.Add,
            "Update" => Event.Update,
            "Remove" => Event.Remove,
            _ => throw new ArgumentException($"Unknown Event protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out Event result) {
        switch (value) {
            case "Invalid":
                result = Event.Invalid;
                return true;
            case "Add":
                result = Event.Add;
                return true;
            case "Update":
                result = Event.Update;
                return true;
            case "Remove":
                result = Event.Remove;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
