namespace Basalt.Core.Events;

using Basalt.Core.Commands;
using Basalt.Core.Player;

/// <summary>
/// Emitted when a player executes a command. Can be cancelled to prevent execution.
/// </summary>
public sealed class PlayerCommandSignal : PlayerSignal {
    public override ServerEvent Event => ServerEvent.PlayerCommand;

    /// <summary>
    /// The full command line as typed by the player (including the  /).
    /// </summary>
    public string CommandLine { get; }

    /// <summary>
    /// The matched command definition, or null if the command was not found.
    /// </summary>
    public CommandDefinition? Definition { get; }

    /// <summary>
    /// Whether this signal has been cancelled.
    /// </summary>
    public bool Cancelled { get; private set; }

    public PlayerCommandSignal(Player player, string commandLine, CommandDefinition? definition) : base(player) {
        CommandLine = commandLine;
        Definition = definition;
    }

    public void Cancel() {
        Cancelled = true;
    }
}
