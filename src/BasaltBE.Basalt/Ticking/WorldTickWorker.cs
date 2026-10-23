using System.Diagnostics;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Logging;

namespace BasaltBE.Basalt.Ticking;

internal sealed class WorldTickWorker : IDisposable {
    private int disposed;
    private readonly Server server;
    private World[] worlds;
    private readonly Thread thread;
    private readonly ManualResetEventSlim stop = new(false);
    private readonly ManualResetEventSlim ready = new(false);
    private int ownerThread;

    internal WorldTickWorker(Server server, int index, World[] worlds) {
        this.server = server;
        this.worlds = worlds;
        thread = new Thread(Run) { IsBackground = true, Name = $"Basalt tick {index + 1}" };
    }

    internal void Start() {
        thread.Start();
        ready.Wait();
        Logger.Scoped("Ticking").Info($"{thread.Name}: {string.Join(", ", worlds.Select(world => world.Identifier))}.");
    }

    internal void Stop() => stop.Set();

    internal int WorldCount => worlds.Length;

    internal void AddWorld(World world) {
        world.OwnerThread = ownerThread;
        world.Started = true;
        worlds = [.. worlds, world];
    }

    private void Run() {
        Profiler.SetThreadName(thread.Name!);
        ownerThread = Environment.CurrentManagedThreadId;
        foreach (World world in worlds)
            world.OwnerThread = ownerThread;
        ready.Set();
        long interval = Stopwatch.Frequency / 20;
        long deadline = Stopwatch.GetTimestamp();
        while (!stop.IsSet && !server.Stopping) {
            server.Gameplay.EnterReadLock();
            try {
                foreach (World world in worlds)
                    world.Tick();
            }
            catch (Exception error) {
                Logger.Scoped("Ticking").Error($"{thread.Name} failed: {error}");
                server.Stop();
                break;
            }
            finally {
                server.Gameplay.ExitReadLock();
            }
            deadline += interval;
            long now = Stopwatch.GetTimestamp();
            if (now >= deadline)
                deadline = now + interval;
            int wait = (int)Math.Ceiling((deadline - now) * 1000.0 / Stopwatch.Frequency);
            if (stop.Wait(wait))
                break;
        }
    }

    public void Dispose() {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;
        stop.Set();
        if (thread.IsAlive)
            thread.Join();
        stop.Dispose();
        ready.Dispose();
    }
}
