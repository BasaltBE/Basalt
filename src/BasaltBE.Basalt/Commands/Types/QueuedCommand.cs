using System.Diagnostics;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Commands.Types;

internal readonly struct QueuedCommand(
    CommandExecutor executor,
    string command,
    CommandOriginData origin,
    Action<CommandResult>? completed
) {
    internal readonly CommandExecutor Executor = executor;
    internal readonly string Command = command;
    internal readonly CommandOriginData Origin = origin;
    internal readonly Action<CommandResult>? Completed = completed;
    internal readonly long Created = Stopwatch.GetTimestamp();
}
