namespace Basalt.Core.Network.Handlers;

using Basalt.Core.Tasks;
using Basalt.Core.Worlds.Dimensions;

internal sealed class ResourcePackCoordinatorTask : ServerTask {
    private readonly Server _server;
    private readonly NetworkConnection _connection;
    private readonly Player.Player _player;
    private Dimension? _dimension;

    public ResourcePackCoordinatorTask(
        Server server,
        NetworkConnection connection,
        Player.Player player) {
        _server = server;
        _connection = connection;
        _player = player;
        RunOnMainThread = true;
    }

    public override void Execute() {
        _dimension = ResourcePackCompletedTask.ResolvePlayerDimension(_server, _player);
    }

    public override void Complete() {
        _server.Scheduler.Schedule(new ResourcePackCompletedTask(
            _server,
            _connection,
            _player,
            _dimension));
    }
}
