using System;

namespace BedrockProtocol.Enums;

public enum PlayStatus {
    LoginSuccess = 0,
    LoginFailed_ClientOld = 1,
    LoginFailed_ServerOld = 2,
    PlayerSpawn = 3,
    LoginFailed_InvalidTenant = 4,
    LoginFailed_EditionMismatchEduToVanilla = 5,
    LoginFailed_EditionMismatchVanillaToEdu = 6,
    LoginFailed_ServerFullSubClient = 7,
    LoginFailed_EditorMismatchEditorToVanilla = 8,
    LoginFailed_EditorMismatchVanillaToEditor = 9,
}

public static class PlayStatusExtensions {
    public static string ToProtoString(this PlayStatus value) => value.ToProtocolString();

    public static string ToProtocolString(this PlayStatus value) {
        return value switch {
            PlayStatus.LoginSuccess => "LoginSuccess",
            PlayStatus.LoginFailed_ClientOld => "LoginFailed_ClientOld",
            PlayStatus.LoginFailed_ServerOld => "LoginFailed_ServerOld",
            PlayStatus.PlayerSpawn => "PlayerSpawn",
            PlayStatus.LoginFailed_InvalidTenant => "LoginFailed_InvalidTenant",
            PlayStatus.LoginFailed_EditionMismatchEduToVanilla => "LoginFailed_EditionMismatchEduToVanilla",
            PlayStatus.LoginFailed_EditionMismatchVanillaToEdu => "LoginFailed_EditionMismatchVanillaToEdu",
            PlayStatus.LoginFailed_ServerFullSubClient => "LoginFailed_ServerFullSubClient",
            PlayStatus.LoginFailed_EditorMismatchEditorToVanilla => "LoginFailed_EditorMismatchEditorToVanilla",
            PlayStatus.LoginFailed_EditorMismatchVanillaToEditor => "LoginFailed_EditorMismatchVanillaToEditor",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PlayStatus value.")
        };
    }

    public static PlayStatus FromProtocolString(string value) {
        return value switch {
            "LoginSuccess" => PlayStatus.LoginSuccess,
            "LoginFailed_ClientOld" => PlayStatus.LoginFailed_ClientOld,
            "LoginFailed_ServerOld" => PlayStatus.LoginFailed_ServerOld,
            "PlayerSpawn" => PlayStatus.PlayerSpawn,
            "LoginFailed_InvalidTenant" => PlayStatus.LoginFailed_InvalidTenant,
            "LoginFailed_EditionMismatchEduToVanilla" => PlayStatus.LoginFailed_EditionMismatchEduToVanilla,
            "LoginFailed_EditionMismatchVanillaToEdu" => PlayStatus.LoginFailed_EditionMismatchVanillaToEdu,
            "LoginFailed_ServerFullSubClient" => PlayStatus.LoginFailed_ServerFullSubClient,
            "LoginFailed_EditorMismatchEditorToVanilla" => PlayStatus.LoginFailed_EditorMismatchEditorToVanilla,
            "LoginFailed_EditorMismatchVanillaToEditor" => PlayStatus.LoginFailed_EditorMismatchVanillaToEditor,
            _ => throw new ArgumentException($"Unknown PlayStatus protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PlayStatus result) {
        switch (value) {
            case "LoginSuccess":
                result = PlayStatus.LoginSuccess;
                return true;
            case "LoginFailed_ClientOld":
                result = PlayStatus.LoginFailed_ClientOld;
                return true;
            case "LoginFailed_ServerOld":
                result = PlayStatus.LoginFailed_ServerOld;
                return true;
            case "PlayerSpawn":
                result = PlayStatus.PlayerSpawn;
                return true;
            case "LoginFailed_InvalidTenant":
                result = PlayStatus.LoginFailed_InvalidTenant;
                return true;
            case "LoginFailed_EditionMismatchEduToVanilla":
                result = PlayStatus.LoginFailed_EditionMismatchEduToVanilla;
                return true;
            case "LoginFailed_EditionMismatchVanillaToEdu":
                result = PlayStatus.LoginFailed_EditionMismatchVanillaToEdu;
                return true;
            case "LoginFailed_ServerFullSubClient":
                result = PlayStatus.LoginFailed_ServerFullSubClient;
                return true;
            case "LoginFailed_EditorMismatchEditorToVanilla":
                result = PlayStatus.LoginFailed_EditorMismatchEditorToVanilla;
                return true;
            case "LoginFailed_EditorMismatchVanillaToEditor":
                result = PlayStatus.LoginFailed_EditorMismatchVanillaToEditor;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
