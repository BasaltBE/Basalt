#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum ServerWaypointGroupAction {
    None = 0,
    Add = 1,
    Remove = 2,
    Update = 3,
}

public static class ServerWaypointGroupActionExtensions {
    public static string ToProtoString(this ServerWaypointGroupAction value) => value.ToProtocolString();

    public static string ToProtocolString(this ServerWaypointGroupAction value) {
        return value switch {
            ServerWaypointGroupAction.None => "None",
            ServerWaypointGroupAction.Add => "Add",
            ServerWaypointGroupAction.Remove => "Remove",
            ServerWaypointGroupAction.Update => "Update",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ServerWaypointGroupAction value.")
        };
    }

    public static ServerWaypointGroupAction FromProtocolString(string value) {
        return value switch {
            "None" => ServerWaypointGroupAction.None,
            "Add" => ServerWaypointGroupAction.Add,
            "Remove" => ServerWaypointGroupAction.Remove,
            "Update" => ServerWaypointGroupAction.Update,
            _ => throw new ArgumentException($"Unknown ServerWaypointGroupAction protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ServerWaypointGroupAction result) {
        switch (value) {
            case "None":
                result = ServerWaypointGroupAction.None;
                return true;
            case "Add":
                result = ServerWaypointGroupAction.Add;
                return true;
            case "Remove":
                result = ServerWaypointGroupAction.Remove;
                return true;
            case "Update":
                result = ServerWaypointGroupAction.Update;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
