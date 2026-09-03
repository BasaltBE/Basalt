namespace Basalt.Core.Tasks;

using System.Collections.Concurrent;
using Basalt.Core.Plugins;
using Basalt.Core.Profiling;

public sealed class TaskScheduler {
    private const int DeferredDomainCapacity = 4096;
    private readonly TaskWorkerPool _workerPool;
    private readonly ConcurrentQueue<ServerTask> _mainThreadQueue = new();
    private readonly ConcurrentQueue<ServerTask> _mainThreadCompletionQueue = new();
    private readonly ConcurrentQueue<ServerTask> _deferredWorkerQueue = new();
    private readonly ConcurrentQueue<ServerTask> _deferredDomainQueue = new();
    private readonly List<DelayedTask> _delayedTasks = [];
    private readonly List<RepeatingTask> _repeatingTasks = [];
    private readonly object _scheduleLock = new();
    private readonly Func<PluginContainer?>? _ownerProvider;
    private readonly Action<ServerTask, PluginContainer?>? _taskConfigurator;
    private int _deferredDomainCount;

    public int PendingDeferredWorkCount => _deferredWorkerQueue.Count;
    public int PendingDeferredDomainWorkCount => Volatile.Read(ref _deferredDomainCount);

    public TaskScheduler(
        TaskWorkerPool workerPool,
        Func<PluginContainer?>? ownerProvider = null,
        Action<ServerTask, PluginContainer?>? taskConfigurator = null) {
        _workerPool = workerPool;
        _ownerProvider = ownerProvider;
        _taskConfigurator = taskConfigurator;
    }

    public void Schedule(ServerTask task) {
        ConfigureTask(task);
        if (task.ExecutionMailbox is { } mailbox) {
            if (!mailbox.TryEnqueue(task.ExecuteOnDomain, task.Cancel)) {
                if (!TryDeferDomain(task)) {
                    task.Cancel();
                }
            }
        }
        else if (task.RunOnMainThread) {
            _mainThreadQueue.Enqueue(task);
        }
        else if (!_workerPool.TryEnqueue(task)) {
            _deferredWorkerQueue.Enqueue(task);
        }
    }

    public void Schedule(DelayedTask task, ulong currentTick) {
        ConfigureTask(task);
        task.ExecutionTick = currentTick + task.DelayTicks;

        lock (_scheduleLock) {
            _delayedTasks.Add(task);
        }
    }

    public void Schedule(RepeatingTask task, ulong currentTick) {
        ConfigureTask(task);
        task.NextExecutionTick = currentTick + task.IntervalTicks;

        lock (_scheduleLock) {
            _repeatingTasks.Add(task);
        }
    }

    public void Tick(ulong currentTick) {
        using var _ = Profiler.Enabled ? Profiler.BeginZone("TaskScheduler.Process") : default;
        while (_deferredWorkerQueue.TryDequeue(out ServerTask? deferred)) {
            if (!_workerPool.TryEnqueue(deferred)) {
                _deferredWorkerQueue.Enqueue(deferred);
                break;
            }
        }

        while (_deferredDomainQueue.TryPeek(out ServerTask? deferred)) {
            if (deferred.ExecutionMailbox is not { } mailbox) {
                DequeueDeferredDomain(out ServerTask? discarded);
                discarded!.Cancel();
                discarded.MarkCompleted();
                continue;
            }

            if (!mailbox.TryEnqueue(deferred.ExecuteOnDomain, deferred.Cancel)) {
                if (mailbox.IsCompleted) {
                    DequeueDeferredDomain(out ServerTask? discarded);
                    discarded!.Cancel();
                    discarded.MarkCompleted();
                    continue;
                }
                break;
            }

            DequeueDeferredDomain(out ServerTask? removed);
        }

        lock (_scheduleLock) {
            for (int i = _delayedTasks.Count - 1; i >= 0; i--) {
                DelayedTask task = _delayedTasks[i];
                if (task.IsCancelled) {
                    _delayedTasks.RemoveAt(i);
                    continue;
                }

                if (currentTick >= task.ExecutionTick) {
                    _delayedTasks.RemoveAt(i);
                    DispatchTask(task);
                }
            }

            for (int i = _repeatingTasks.Count - 1; i >= 0; i--) {
                RepeatingTask task = _repeatingTasks[i];
                if (task.IsCancelled) {
                    _repeatingTasks.RemoveAt(i);
                    continue;
                }

                if (currentTick >= task.NextExecutionTick) {
                    task.NextExecutionTick = currentTick + task.IntervalTicks;
                    DispatchTask(task);
                }
            }
        }

        while (_mainThreadQueue.TryDequeue(out ServerTask? task)) {
            if (task.IsCancelled) continue;

            bool succeeded = true;
            using (Profiler.Enabled ? Profiler.BeginZone($"MainThread:{task.GetType().Name}") : default) {
                try {
                    task.Execute();
                }
                catch (Exception ex) {
                    succeeded = false;
                    task.ExecutionFailed = true;
                    task.ReportFailure("task execution", ex);
                }
            }

            task.IsExecuted = true;
            if (succeeded) {
                _mainThreadCompletionQueue.Enqueue(task);
            }
            else {
                task.MarkCompleted();
            }
        }

        while (_mainThreadCompletionQueue.TryDequeue(out ServerTask? task)) {
            if (task.IsCancelled) continue;
            try {
                task.Complete();
            }
            catch (Exception exception) {
                task.ReportFailure("task completion", exception);
            }
            task.MarkCompleted();
        }

        _workerPool.DrainCompletions();
    }

    private void DispatchTask(ServerTask task) {
        if (task.ExecutionMailbox is { } mailbox) {
            if (!mailbox.TryEnqueue(task.ExecuteOnDomain, task.Cancel)) {
                if (!TryDeferDomain(task)) {
                    task.Cancel();
                }
            }
        }
        else if (task.RunOnMainThread) {
            _mainThreadQueue.Enqueue(task);
        }
        else if (!_workerPool.TryEnqueue(task)) {
            _deferredWorkerQueue.Enqueue(task);
        }
    }

    private void ConfigureTask(ServerTask task) {
        PluginContainer? owner = task.Owner ?? _ownerProvider?.Invoke();
        _taskConfigurator?.Invoke(task, owner);
    }

    private bool TryDeferDomain(ServerTask task) {
        while (true) {
            int count = Volatile.Read(ref _deferredDomainCount);
            if (count >= DeferredDomainCapacity) {
                return false;
            }

            if (Interlocked.CompareExchange(ref _deferredDomainCount, count + 1, count) == count) {
                _deferredDomainQueue.Enqueue(task);
                return true;
            }
        }
    }

    private bool DequeueDeferredDomain(out ServerTask? task) {
        if (!_deferredDomainQueue.TryDequeue(out task)) {
            return false;
        }

        Interlocked.Decrement(ref _deferredDomainCount);
        return true;
    }
}
