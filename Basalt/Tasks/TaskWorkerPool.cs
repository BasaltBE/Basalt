namespace Basalt.Core.Tasks;

using System.Collections.Concurrent;
using Basalt.Core.Profiling;

public sealed class TaskWorkerPool : IDisposable {
    [ThreadStatic]
    private static bool _workerThread;

    private readonly Thread[] _workers;
    private readonly BlockingCollection<ServerTask> _workQueue = new();
    private readonly ConcurrentQueue<ServerTask> _completionQueue = new();

    public int WorkerCount => _workers.Length;
    internal static bool WorkerThread => _workerThread;

    public TaskWorkerPool(int workerCount = 4) {
        _workers = new Thread[workerCount];
        for (int i = 0; i < workerCount; i++) {
            int index = i;
            _workers[i] = new Thread(() => WorkerLoop(index)) {
                Name = $"BasaltWorker-{i}",
                IsBackground = true
            };
            _workers[i].Start();
        }
    }

    internal void Enqueue(ServerTask task) {
        if (task.IsCancelled) return;
        _workQueue.Add(task);
    }

    internal void DrainCompletions() {
        using var _ = Profiler.Enabled ? Profiler.BeginZone("WorkerPool.DrainCompletions") : default;
        while (_completionQueue.TryDequeue(out ServerTask? task)) {
            if (task.IsCancelled) continue;
            task.Complete();
            task.IsCompleted = true;
        }
    }

    private void WorkerLoop(int index) {
        _workerThread = true;
        Profiler.SetThreadName($"BasaltWorker-{index}");
        foreach (ServerTask task in _workQueue.GetConsumingEnumerable()) {
            if (task.IsCancelled) {
                continue;
            }

            using (Profiler.Enabled ? Profiler.BeginZone(task.GetType().Name) : default) {
                try {
                    task.Execute();
                }
                catch (Exception ex) {
                    Logger.Warn($"Task execution failed: {ex}");
                }
            }

            task.IsExecuted = true;

            if (task.MainThreadCompletion) {
                _completionQueue.Enqueue(task);
            }
            else {
                task.IsCompleted = true;
            }
        }
    }

    public void Dispose() {
        _workQueue.CompleteAdding();
        foreach (Thread worker in _workers) {
            worker.Join(1000);
        }
        _workQueue.Dispose();
    }
}
