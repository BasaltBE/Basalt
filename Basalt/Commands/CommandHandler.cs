namespace Basalt.Core.Commands;

/// <summary>
/// Delegate for command execution.
/// </summary>
public delegate CommandResult CommandExecuteDelegate(CommandContext ctx);

/// <summary>
/// Wraps a command execution function.
/// </summary>
public sealed class CommandHandler {
    readonly CommandExecuteDelegate _execute;

    public CommandHandler(CommandExecuteDelegate execute) {
        _execute = execute;
    }

    public CommandResult Execute(CommandContext ctx) => _execute(ctx);
}
