namespace Basalt.Protocol.Enums;

/// <summary>
/// Identifies where a command request came from.
/// </summary>
public enum CommandOriginType : uint {
    /// <summary>
    /// Command came from a player.
    /// </summary>
    Player,
    /// <summary>
    /// Command came from a command block.
    /// </summary>
    CommandBlock,
    /// <summary>
    /// Command came from a command block minecart.
    /// </summary>
    MinecartCommandBlock,
    /// <summary>
    /// Command came from the developer console.
    /// </summary>
    DevConsole,
    /// <summary>
    /// Command came from a test source.
    /// </summary>
    Test,
    /// <summary>
    /// Command came from an automation player.
    /// </summary>
    AutomationPlayer,
    /// <summary>
    /// Command came from client automation.
    /// </summary>
    ClientAutomation,
    /// <summary>
    /// Command came from the dedicated server.
    /// </summary>
    DedicatedServer,
    /// <summary>
    /// Command came from an entity.
    /// </summary>
    Entity,
    /// <summary>
    /// Command came from a virtual source.
    /// </summary>
    Virtual,
    /// <summary>
    /// Command came from a game argument source.
    /// </summary>
    GameArgument,
    /// <summary>
    /// Command came from a server-side entity source.
    /// </summary>
    EntityServer,
    /// <summary>
    /// Command came from a precompiled source.
    /// </summary>
    Precompiled,
    /// <summary>
    /// Command came from a game director entity source.
    /// </summary>
    GameDirectorEntityServer,
    /// <summary>
    /// Command came from a script.
    /// </summary>
    Script,
    /// <summary>
    /// Command came from an execution context.
    /// </summary>
    Executor
}
