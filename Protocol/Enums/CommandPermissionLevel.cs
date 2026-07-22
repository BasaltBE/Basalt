namespace Basalt.Protocol.Enums;

/// <summary>
/// Permission level advertised for an available command.
/// </summary>
public enum CommandPermissionLevel : byte {
    /// <summary>
    /// Command is available to any permission level.
    /// </summary>
    Any,
    /// <summary>
    /// Command is available to game directors.
    /// </summary>
    GameDirectors,
    /// <summary>
    /// Command is available to admins.
    /// </summary>
    Admin,
    /// <summary>
    /// Command is available to hosts.
    /// </summary>
    Host,
    /// <summary>
    /// Command is available to owners.
    /// </summary>
    Owner,
    /// <summary>
    /// Command is available to internal sources.
    /// </summary>
    Internal
}
