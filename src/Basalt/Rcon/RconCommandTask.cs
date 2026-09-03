namespace Basalt.Core.Rcon;

using Basalt.Core.Commands;
using Basalt.Core.Tasks;

public sealed class RconCommandTask : ServerTask {
    readonly Server _server;
    readonly string _command;
    readonly TaskCompletionSource<CommandResult> _completion = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    public RconCommandTask(Server server, string command) {
        _server = server;
        _command = command;
        RunOnMainThread = true;
    }

    public Task<CommandResult> Completion => _completion.Task;

    public override void Execute() {
        try {
            _completion.TrySetResult(_server.Commands.Execute(_server, _command));
        }
        catch (Exception exception) {
            _completion.TrySetResult(CommandResult.Error(exception.Message));
        }
    }
}
