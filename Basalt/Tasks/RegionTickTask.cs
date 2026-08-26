namespace Basalt.Core.Tasks;

internal sealed class RegionTickTask : ServerTask {
    private readonly Action _tick;
    private readonly ManualResetEventSlim _completed;

    public Exception? Error { get; private set; }

    public RegionTickTask(Action tick, ManualResetEventSlim completed) {
        _tick = tick;
        _completed = completed;
        MainThreadCompletion = false;
    }

    public override void Execute() {
        try {
            _tick();
        }
        catch (Exception exception) {
            Error = exception;
        }
        finally {
            _completed.Set();
        }
    }
}
