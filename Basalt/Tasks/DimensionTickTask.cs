namespace Basalt.Core.Tasks;

using Basalt.Core.Worlds.Dimensions;

internal sealed class DimensionTickTask : ServerTask {
    private readonly Dimension _dimension;
    private readonly ulong _currentTick;
    private readonly uint _deltaTick;
    private readonly ManualResetEventSlim _completed;

    public Exception? Error { get; private set; }

    public DimensionTickTask(
        Dimension dimension,
        ulong currentTick,
        uint deltaTick,
        ManualResetEventSlim completed) {
        _dimension = dimension;
        _currentTick = currentTick;
        _deltaTick = deltaTick;
        _completed = completed;
        MainThreadCompletion = false;
    }

    public override void Execute() {
        try {
            _dimension.Tick(_currentTick, _deltaTick);
        }
        catch (Exception exception) {
            Error = exception;
        }
        finally {
            _completed.Set();
        }
    }
}
