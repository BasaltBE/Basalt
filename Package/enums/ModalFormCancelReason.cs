using System;

namespace BedrockProtocol.Enums;

public enum ModalFormCancelReason {
    UserClosed = 0,
    UserBusy = 1,
}

public static class ModalFormCancelReasonExtensions {
    public static string ToProtoString(this ModalFormCancelReason value) => value.ToProtocolString();

    public static string ToProtocolString(this ModalFormCancelReason value) {
        return value switch {
            ModalFormCancelReason.UserClosed => "UserClosed",
            ModalFormCancelReason.UserBusy => "UserBusy",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ModalFormCancelReason value.")
        };
    }

    public static ModalFormCancelReason FromProtocolString(string value) {
        return value switch {
            "UserClosed" => ModalFormCancelReason.UserClosed,
            "UserBusy" => ModalFormCancelReason.UserBusy,
            _ => throw new ArgumentException($"Unknown ModalFormCancelReason protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ModalFormCancelReason result) {
        switch (value) {
            case "UserClosed":
                result = ModalFormCancelReason.UserClosed;
                return true;
            case "UserBusy":
                result = ModalFormCancelReason.UserBusy;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
