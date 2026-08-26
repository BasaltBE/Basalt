namespace Basalt.Core.Tasks;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Basalt.Core.Profiling;

public sealed class TaskWorkerPool : IDisposable {
    private const int WorkItemsPerWorker = 256;
    [ThreadStatic]
    private static bool _workerThread;
    [ThreadStatic]
    private static int _workerIndex;

    private readonly Thread[] _workers;
    private readonly PriorityTaskQueue _workQueue;
    private readonly ConcurrentQueue<ServerTask> _completionQueue = new();
    private long _queueWaitTicks;
    private long _queueWaitSamples;

    public int WorkerCount => _workers.Length;
    public int PendingWorkCount => _workQueue.Count;
    public int PendingCompletionCount => _completionQueue.Count;
    public double AverageQueueWaitMilliseconds =>
        Volatile.Read(ref _queueWaitSamples) == 0
            ? 0
            : Volatile.Read(ref _queueWaitTicks) * 1000.0 /
              Stopwatch.Frequency /
              Volatile.Read(ref _queueWaitSamples);
    internal static bool WorkerThread => _workerThread;
    internal static int CurrentWorkerIndex => _workerIndex;

    public TaskWorkerPool(int workerCount = 4) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workerCount);
        _workQueue = new PriorityTaskQueue(workerCount * WorkItemsPerWorker, workerCount);
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

    internal bool TryEnqueue(ServerTask task) {
        if (task.IsCancelled) return true;
        if (task.WorkerAffinity < 0 && task.CompletionMailbox is { } mailbox) {
            task.WorkerAffinity = (RuntimeHelpers.GetHashCode(mailbox) & int.MaxValue) % _workers.Length;
        }

        task.QueuedTimestamp = Stopwatch.GetTimestamp();
        return _workQueue.TryAdd(task);
    }

    internal void DrainCompletions() {
        using var _ = Profiler.Enabled ? Profiler.BeginZone("WorkerPool.DrainCompletions") : default;
        while (_completionQueue.TryDequeue(out ServerTask? task)) {
            if (task.IsCancelled) continue;

            if (task.CompletionMailbox is { } mailbox) {
                if (!mailbox.TryEnqueue(() => CompleteTask(task), task.Cancel)) {
                    if (mailbox.IsCompleted) {
                        task.IsCompleted = true;
                    }
                    else {
                        _completionQueue.Enqueue(task);
                        break;
                    }
                }
                continue;
            }

            CompleteTask(task);
        }
    }

    private static void CompleteTask(ServerTask task) {
        try {
            task.Complete();
        }
        finally {
            task.IsCompleted = true;
        }
    }

    private void WorkerLoop(int index) {
        _workerThread = true;
        _workerIndex = index;
        Profiler.SetThreadName($"BasaltWorker-{index}");
        while (_workQueue.TryTake(index, out ServerTask task)) {
            if (task.IsCancelled) {
                continue;
            }

            long queuedTimestamp = Interlocked.Exchange(ref task.QueuedTimestamp, 0);
            if (queuedTimestamp != 0) {
                Interlocked.Add(ref _queueWaitTicks, Stopwatch.GetTimestamp() - queuedTimestamp);
                Interlocked.Increment(ref _queueWaitSamples);
            }

            bool succeeded = true;
            using (Profiler.Enabled ? Profiler.BeginZone(task.GetType().Name) : default) {
                try {
                    task.Execute();
                }
                catch (Exception ex) {
                    succeeded = false;
                    task.ExecutionFailed = true;
                    Logger.Warn($"Task execution failed: {ex}");
                }
            }

            task.IsExecuted = true;

            if (!succeeded) {
                task.IsCompleted = true;
            }
            else if (task.CompletionMailbox is { } mailbox) {
                if (!mailbox.TryEnqueue(() => CompleteTask(task), task.Cancel)) {
                    if (mailbox.IsCompleted) {
                        task.IsCompleted = true;
                    }
                    else {
                        _completionQueue.Enqueue(task);
                    }
                }
            }
            else if (task.MainThreadCompletion) {
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
    }
}
