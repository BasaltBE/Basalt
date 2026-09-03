namespace Basalt.Core.Tasks;

using System.Diagnostics;
using Basalt.Core.Worlds;

internal sealed class WorldTickTask : ServerTask {
    private readonly World _world;
    private readonly ManualResetEventSlim _completed;

    public Exception? Error { get; private set; }
    public double ElapsedMilliseconds { get; private set; }

    public WorldTickTask(World world, ManualResetEventSlim completed) {
        _world = world;
        _completed = completed;
        MainThreadCompletion = false;
    }

    public override void Execute() {
        long startTimestamp = Stopwatch.GetTimestamp();
        try {
            _world.Tick();
        }
        catch (Exception exception) {
            Error = exception;
        }
        finally {
            ElapsedMilliseconds = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
            _completed.Set();
        }
    }
}
