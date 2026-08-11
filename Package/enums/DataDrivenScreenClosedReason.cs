using System;

namespace BedrockProtocol.Enums;

public enum DataDrivenScreenClosedReason {
    ProgrammaticClose = 0,
    ProgrammaticCloseAll = 1,
    ClientCanceled = 2,
    UserBusy = 3,
    InvalidForm = 4,
}

public static class DataDrivenScreenClosedReasonExtensions {
    public static string ToProtoString(this DataDrivenScreenClosedReason value) => value.ToProtocolString();

    public static string ToProtocolString(this DataDrivenScreenClosedReason value) {
        return value switch {
            DataDrivenScreenClosedReason.ProgrammaticClose => "ProgrammaticClose",
            DataDrivenScreenClosedReason.ProgrammaticCloseAll => "ProgrammaticCloseAll",
            DataDrivenScreenClosedReason.ClientCanceled => "ClientCanceled",
            DataDrivenScreenClosedReason.UserBusy => "UserBusy",
            DataDrivenScreenClosedReason.InvalidForm => "InvalidForm",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown DataDrivenScreenClosedReason value.")
        };
    }

    public static DataDrivenScreenClosedReason FromProtocolString(string value) {
        return value switch {
            "ProgrammaticClose" => DataDrivenScreenClosedReason.ProgrammaticClose,
            "ProgrammaticCloseAll" => DataDrivenScreenClosedReason.ProgrammaticCloseAll,
            "ClientCanceled" => DataDrivenScreenClosedReason.ClientCanceled,
            "UserBusy" => DataDrivenScreenClosedReason.UserBusy,
            "InvalidForm" => DataDrivenScreenClosedReason.InvalidForm,
            _ => throw new ArgumentException($"Unknown DataDrivenScreenClosedReason protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out DataDrivenScreenClosedReason result) {
        switch (value) {
            case "ProgrammaticClose":
                result = DataDrivenScreenClosedReason.ProgrammaticClose;
                return true;
            case "ProgrammaticCloseAll":
                result = DataDrivenScreenClosedReason.ProgrammaticCloseAll;
                return true;
            case "ClientCanceled":
                result = DataDrivenScreenClosedReason.ClientCanceled;
                return true;
            case "UserBusy":
                result = DataDrivenScreenClosedReason.UserBusy;
                return true;
            case "InvalidForm":
                result = DataDrivenScreenClosedReason.InvalidForm;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
