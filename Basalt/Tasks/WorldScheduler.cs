namespace Basalt.Core.Tasks;

using System.Collections.Concurrent;
using Basalt.Core.Profiling;
using Basalt.Core.Worlds;

public sealed class WorldScheduler {
    private const int WheelSize = 1200;
    private const int DeferredDomainCapacity = 4096;

    private readonly World _world;
    private readonly TaskWorkerPool _workerPool;
    private readonly ConcurrentQueue<ServerTask> _incoming = new();
    private readonly Queue<ServerTask> _deferredWorkerTasks = new();
    private readonly Queue<ServerTask> _deferredDomainTasks = new();
    private readonly TickWheelSlot[] _wheel = new TickWheelSlot[WheelSize];
    private RepeatingTask[] _repeating = new RepeatingTask[16];
    private int _repeatingCount;
    private readonly List<ServerTask> _workerBatch = new(64);
    private volatile bool _stopped;

    public bool IsStopped => _stopped;
    public int PendingDeferredWorkCount => _deferredWorkerTasks.Count;
    public int PendingDeferredDomainWorkCount => _deferredDomainTasks.Count;

    public WorldScheduler(World world, TaskWorkerPool workerPool) {
        _world = world;
        _workerPool = workerPool;
    }

    public void Schedule(ServerTask task) {
        if (_stopped) {
            task.Cancel();
            return;
        }

        _incoming.Enqueue(task);
    }

    public void Schedule(DelayedTask task) {
        if (_stopped) {
            task.Cancel();
            return;
        }

        task.ExecutionTick = _world.TickValue + task.DelayTicks;
        _incoming.Enqueue(task);
    }

    public void Schedule(RepeatingTask task) {
        if (_stopped) {
            task.Cancel();
            return;
        }

        task.NextExecutionTick = _world.TickValue + task.IntervalTicks;
        _incoming.Enqueue(task);
    }

    public void Tick() {
        if (_stopped) return;

        using var _ = Profiler.Enabled ? Profiler.BeginZone("WorldScheduler.Tick") : default;
        ulong currentTick = _world.TickValue;

        while (_deferredWorkerTasks.Count > 0 &&
               _workerPool.TryEnqueue(_deferredWorkerTasks.Peek())) {
            _deferredWorkerTasks.Dequeue();
        }

        while (_deferredDomainTasks.Count > 0) {
            ServerTask task = _deferredDomainTasks.Peek();
            if (task.ExecutionMailbox is not { } mailbox) {
                _deferredDomainTasks.Dequeue();
                task.Cancel();
                task.IsCompleted = true;
                continue;
            }

            if (!mailbox.TryEnqueue(task.ExecuteOnDomain, task.Cancel)) {
                if (mailbox.IsCompleted) {
                    _deferredDomainTasks.Dequeue();
                    task.Cancel();
                    task.IsCompleted = true;
                    continue;
                }
                break;
            }

            _deferredDomainTasks.Dequeue();
        }

        DrainIncoming(currentTick);
        DispatchReadyTasks(currentTick);
        ProcessRepeatingTasks(currentTick);
    }

    public void Stop() {
        _stopped = true;

        for (int i = 0; i < WheelSize; i++) {
            ServerTask? task = _wheel[i].Head;
            _wheel[i].Head = null;
            while (task is not null) {
                ServerTask? next = task.NextInSlot;
                task.NextInSlot = null;
                task.OnStop();
                task.Cancel();
                task = next;
            }
        }

        for (int i = _repeatingCount - 1; i >= 0; i--) {
            _repeating[i].OnStop();
            _repeating[i].Cancel();
            _repeating[i] = null!;
        }
        _repeatingCount = 0;

        while (_incoming.TryDequeue(out ServerTask? task)) {
            task.OnStop();
            task.Cancel();
        }

        while (_deferredWorkerTasks.Count > 0) {
            ServerTask deferred = _deferredWorkerTasks.Dequeue();
            deferred.OnStop();
            deferred.Cancel();
        }

        while (_deferredDomainTasks.Count > 0) {
            ServerTask deferred = _deferredDomainTasks.Dequeue();
            deferred.OnStop();
            deferred.Cancel();
        }
    }

    private void DrainIncoming(ulong currentTick) {
        while (_incoming.TryDequeue(out ServerTask? task)) {
            if (task.IsCancelled) continue;

            if (task is RepeatingTask repeating) {
                if (_repeatingCount == _repeating.Length)
                    Array.Resize(ref _repeating, _repeatingCount * 2);

                _repeating[_repeatingCount++] = repeating;
            }
            else if (task is DelayedTask delayed) {
                if (delayed.ExecutionTick == 0)
                    delayed.ExecutionTick = currentTick + delayed.DelayTicks;
                InsertIntoWheel(delayed);
            }
            else {
                // Immediate task: dispatch right away.
                if (task.ExecutionMailbox is { } mailbox) {
                    if (!mailbox.TryEnqueue(task.ExecuteOnDomain, task.Cancel)) {
                        if (!TryDeferDomain(task)) {
                            task.Cancel();
                        }
                    }
                }
                else if (task.RunOnMainThread)
                    ExecuteMainThread(task);
                else if (!_workerPool.TryEnqueue(task) && !task.IsCancelled) {
                    _deferredWorkerTasks.Enqueue(task);
                }
            }
        }
    }

    private void InsertIntoWheel(DelayedTask task) {
        int slot = (int)(task.ExecutionTick % (ulong)WheelSize);
        task.NextInSlot = _wheel[slot].Head;
        _wheel[slot].Head = task;
    }

    private void DispatchReadyTasks(ulong currentTick) {
        int slot = (int)(currentTick % (ulong)WheelSize);
        ServerTask? task = _wheel[slot].Head;
        _wheel[slot].Head = null;

        ServerTask? mainHead = null;
        _workerBatch.Clear();

        while (task is not null) {
            ServerTask? next = task.NextInSlot;
            task.NextInSlot = null;

            if (task.IsCancelled) {
                task = next;
                continue;
            }

            if (task is DelayedTask delayed && delayed.ExecutionTick > currentTick) {
                InsertIntoWheel(delayed);
                task = next;
                continue;
            }

            if (task.ExecutionMailbox is { } mailbox) {
                if (!mailbox.TryEnqueue(task.ExecuteOnDomain, task.Cancel)) {
                    if (!TryDeferDomain(task)) {
                        task.Cancel();
                    }
                }
            }
            else if (task.RunOnMainThread) {
                task.NextInSlot = mainHead;
                mainHead = task;
            }
            else {
                _workerBatch.Add(task);
            }

            task = next;
        }

        // Sort worker batch by priority for fairness.
        if (_workerBatch.Count > 1)
            _workerBatch.Sort(static (a, b) => a.Priority.CompareTo(b.Priority));

        int workerCount = _workerBatch.Count;
        if (workerCount > 0) {
            using (Profiler.Enabled ? Profiler.BeginZone("WorkerDispatch") : default) {
                for (int i = 0; i < workerCount; i++) {
                    ServerTask workerTask = _workerBatch[i];
                    if (!_workerPool.TryEnqueue(workerTask) && !workerTask.IsCancelled) {
                        _deferredWorkerTasks.Enqueue(workerTask);
                    }
                }
            }
            _workerBatch.Clear();
        }

        using (Profiler.Enabled ? Profiler.BeginZone("MainThreadTasks") : default) {
            while (mainHead is not null) {
                ServerTask? next = mainHead.NextInSlot;
                mainHead.NextInSlot = null;

                if (!mainHead.IsCancelled)
                    ExecuteMainThread(mainHead);

                mainHead = next;
            }
        }
    }

    private bool TryDeferDomain(ServerTask task) {
        if (_deferredDomainTasks.Count >= DeferredDomainCapacity) {
            return false;
        }

        _deferredDomainTasks.Enqueue(task);
        return true;
    }

    private void ProcessRepeatingTasks(ulong currentTick) {
        for (int i = _repeatingCount - 1; i >= 0; i--) {
            RepeatingTask rt = _repeating[i];
            if (rt.IsCancelled) {
                RemoveRepeatingAt(i);
                continue;
            }

            if (currentTick < rt.NextExecutionTick) continue;

            rt.NextExecutionTick = currentTick + rt.IntervalTicks;

            if (rt.ExecutionMailbox is { } mailbox) {
                if (!mailbox.TryEnqueue(rt.ExecuteOnDomain, rt.Cancel)) {
                    _deferredDomainTasks.Enqueue(rt);
                }
            }
            else if (rt.RunOnMainThread) {
                ExecuteMainThread(rt);
            }
            else if (!_workerPool.TryEnqueue(rt) && !rt.IsCancelled) {
                _deferredWorkerTasks.Enqueue(rt);
            }
        }
    }

    private void RemoveRepeatingAt(int index) {
        _repeatingCount--;
        _repeating[index] = _repeating[_repeatingCount];
        _repeating[_repeatingCount] = null!;
    }

    private static void ExecuteMainThread(ServerTask task) {
        bool succeeded = true;
        using (Profiler.Enabled ? Profiler.BeginZone($"MainThread:{task.GetType().Name}") : default) {
            try {
                task.Execute();
            }
            catch (Exception ex) {
                succeeded = false;
                task.ExecutionFailed = true;
                Logger.Warn($"Main thread task execution failed: {ex}");
            }
        }

        task.IsExecuted = true;

        if (succeeded) {
            try {
                task.Complete();
            }
            catch (Exception ex) {
                Logger.Warn($"Main thread task Complete() failed: {ex}");
            }
        }

        task.IsCompleted = true;
    }

    internal struct TickWheelSlot {
        public ServerTask? Head;
    }
}
