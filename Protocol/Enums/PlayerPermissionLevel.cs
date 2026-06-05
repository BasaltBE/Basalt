namespace Basalt.Protocol.Enums;

/// <summary>
/// Player permission level shown to the client.
/// </summary>
public enum PlayerPermissionLevel : byte
{
    /// <summary>
    /// Player has visitor permissions.
    /// </summary>
    Visitor,

    /// <summary>
    /// Player has member permissions.
    /// </summary>
    Member,

    /// <summary>
    /// Player has operator permissions.
    /// </summary>
    Operator,

    /// <summary>
    /// Player has custom permissions.
    /// </summary>
    Custom
}
