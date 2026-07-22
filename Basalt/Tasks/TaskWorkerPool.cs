namespace Basalt.Core.Tasks;

using System.Collections.Concurrent;
using Basalt.Core.Profiling;

public sealed class TaskWorkerPool : IDisposable {
  private readonly Thread[] _workers;
  private readonly BlockingCollection<WorkItem> _workQueue = new();
  private readonly ConcurrentQueue<ServerTask> _completionQueue = new();

  public int WorkerCount => _workers.Length;

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
    _workQueue.Add(new WorkItem(task, null));
  }

  internal void Enqueue(ServerTask task, CountdownEvent barrier) {
    if (task.IsCancelled) {
      barrier.Signal();
      return;
    }
    _workQueue.Add(new WorkItem(task, barrier));
  }

  internal void DrainCompletions() {
    using var _ = Profiler.BeginZone("WorkerPool.DrainCompletions");
    while (_completionQueue.TryDequeue(out ServerTask? task)) {
      if (task.IsCancelled) continue;
      task.Complete();
      task.IsCompleted = true;
    }
  }

  private void WorkerLoop(int index) {
    Profiler.SetThreadName($"BasaltWorker-{index}");
    foreach (WorkItem item in _workQueue.GetConsumingEnumerable()) {
      ServerTask task = item.Task;
      CountdownEvent? barrier = item.Barrier;

      if (task.IsCancelled) {
        barrier?.Signal();
        continue;
      }

      using (Profiler.BeginZone(task.GetType().Name)) {
        try {
          task.Execute();
        }
        catch (Exception ex) {
          Logger.Warn($"Task execution failed: {ex}");
        }
      }

      task.IsExecuted = true;

      if (barrier is not null) {
        barrier.Signal();
      }
      else {
        _completionQueue.Enqueue(task);
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

  private readonly record struct WorkItem(ServerTask Task, CountdownEvent? Barrier);
}
