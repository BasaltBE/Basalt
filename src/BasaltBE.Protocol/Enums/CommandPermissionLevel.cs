namespace BasaltBE.Protocol.Enums;

public enum CommandPermissionLevel : byte {
    Any,
    GameDirectors,
    Admin,
    Host,
    Owner,
    Internal
}
