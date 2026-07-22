namespace Basalt.Protocol.Enums;

/// <summary>
/// Layer type used by ability data.
/// </summary>
public enum AbilityLayerType : ushort {
    /// <summary>
    /// Custom cache ability layer.
    /// </summary>
    CustomCache,

    /// <summary>
    /// Base ability layer.
    /// </summary>
    Base,

    /// <summary>
    /// Spectator ability layer.
    /// </summary>
    Spectator,

    /// <summary>
    /// Command ability layer.
    /// </summary>
    Commands,

    /// <summary>
    /// Editor ability layer.
    /// </summary>
    Editor,

    /// <summary>
    /// Loading screen ability layer.
    /// </summary>
    LoadingScreen
}
