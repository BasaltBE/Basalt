namespace Basalt.Core.Tasks;

using System.Collections.Concurrent;
using Basalt.Core.Profiling;

public sealed class TaskScheduler {
    private readonly TaskWorkerPool _workerPool;
    private readonly ConcurrentQueue<ServerTask> _mainThreadQueue = new();
    private readonly ConcurrentQueue<ServerTask> _mainThreadCompletionQueue = new();
    private readonly List<DelayedTask> _delayedTasks = [];
    private readonly List<RepeatingTask> _repeatingTasks = [];
    private readonly object _scheduleLock = new();

    public TaskScheduler(TaskWorkerPool workerPool) {
        _workerPool = workerPool;
    }

    public void Schedule(ServerTask task) {
        task.OwnerThreadId = Environment.CurrentManagedThreadId;

        if (task.RunOnMainThread) {
            _mainThreadQueue.Enqueue(task);
        }
        else {
            _workerPool.Enqueue(task);
        }
    }

    public void Schedule(DelayedTask task, ulong currentTick) {
        task.OwnerThreadId = Environment.CurrentManagedThreadId;
        task.ExecutionTick = currentTick + task.DelayTicks;

        lock (_scheduleLock) {
            _delayedTasks.Add(task);
        }
    }

    public void Schedule(RepeatingTask task, ulong currentTick) {
        task.OwnerThreadId = Environment.CurrentManagedThreadId;
        task.NextExecutionTick = currentTick + task.IntervalTicks;

        lock (_scheduleLock) {
            _repeatingTasks.Add(task);
        }
    }

    public void Tick(ulong currentTick) {
        using var _ = Profiler.Enabled ? Profiler.BeginZone("TaskScheduler.Process") : default;
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

            using (Profiler.Enabled ? Profiler.BeginZone($"MainThread:{task.GetType().Name}") : default) {
                try {
                    task.Execute();
                }
                catch (Exception ex) {
                    Logger.Warn($"Main thread task execution failed: {ex}");
                }
            }

            task.IsExecuted = true;
            _mainThreadCompletionQueue.Enqueue(task);
        }

        while (_mainThreadCompletionQueue.TryDequeue(out ServerTask? task)) {
            if (task.IsCancelled) continue;
            task.Complete();
            task.IsCompleted = true;
        }

        _workerPool.DrainCompletions();
    }

    private void DispatchTask(ServerTask task) {
        if (task.RunOnMainThread) {
            _mainThreadQueue.Enqueue(task);
        }
        else {
            _workerPool.Enqueue(task);
        }
    }
}
