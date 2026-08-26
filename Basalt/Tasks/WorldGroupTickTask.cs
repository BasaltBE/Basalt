namespace Basalt.Core.Tasks;

using System.Diagnostics;
using Basalt.Core.Worlds;

internal sealed class WorldGroupTickTask : ServerTask {
    private readonly World[] _worlds;
    private readonly ManualResetEventSlim _completed;

    public Exception? Error { get; private set; }

    public WorldGroupTickTask(World[] worlds, ManualResetEventSlim completed) {
        _worlds = worlds;
        _completed = completed;
        MainThreadCompletion = false;
    }

    public override void Execute() {
        try {
            for (int i = 0; i < _worlds.Length; i++) {
                World world = _worlds[i];
                long startTimestamp = Stopwatch.GetTimestamp();
                try {
                    world.Tick();
                }
                catch (Exception exception) {
                    Error ??= exception;
                }

                ((Tickable)world).TickWork = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
            }
        }
        finally {
            _completed.Set();
        }
    }
}
