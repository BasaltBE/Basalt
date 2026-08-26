namespace Basalt.Core.Tasks;

internal sealed class PriorityTaskQueue {
    private readonly object _lock = new();
    private readonly PriorityQueue<ServerTask, (TaskPriority Priority, long Sequence)> _queue = new();
    private readonly PriorityQueue<ServerTask, (TaskPriority Priority, long Sequence)>[] _affinityQueues;
    private readonly int _capacity;
    private long _sequence;
    private int _count;
    private bool _addingCompleted;

    public PriorityTaskQueue(int capacity, int workerCount = 1) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workerCount);
        _capacity = capacity;
        _affinityQueues = new PriorityQueue<ServerTask, (TaskPriority Priority, long Sequence)>[workerCount];
        for (int i = 0; i < workerCount; i++) {
            _affinityQueues[i] = new();
        }
    }

    public int Count {
        get {
            lock (_lock) {
                return _count;
            }
        }
    }

    public bool IsAddingCompleted {
        get {
            lock (_lock) {
                return _addingCompleted;
            }
        }
    }

    public bool TryAdd(ServerTask task) {
        lock (_lock) {
            if (_addingCompleted || _count >= _capacity) {
                return false;
            }

            (TaskPriority Priority, long Sequence) priority = (task.Priority, _sequence++);
            if ((uint)task.WorkerAffinity < (uint)_affinityQueues.Length) {
                _affinityQueues[task.WorkerAffinity].Enqueue(task, priority);
            }
            else {
                _queue.Enqueue(task, priority);
            }

            _count++;
            Monitor.Pulse(_lock);
            return true;
        }
    }

    public bool TryTake(out ServerTask task) {
        return TryTake(-1, out task);
    }

    public bool TryTake(int workerIndex, out ServerTask task) {
        lock (_lock) {
            while (_count == 0 && !_addingCompleted) {
                Monitor.Wait(_lock);
            }

            if (_count == 0) {
                task = null!;
                return false;
            }

            TaskPriority? bestPriority = null;
            if (_queue.Count > 0) {
                bestPriority = _queue.Peek().Priority;
            }

            for (int i = 0; i < _affinityQueues.Length; i++) {
                if (_affinityQueues[i].Count > 0 &&
                    (bestPriority is null || _affinityQueues[i].Peek().Priority < bestPriority.Value)) {
                    bestPriority = _affinityQueues[i].Peek().Priority;
                }
            }

            if ((uint)workerIndex < (uint)_affinityQueues.Length &&
                _affinityQueues[workerIndex].Count > 0 &&
                _affinityQueues[workerIndex].Peek().Priority == bestPriority) {
                task = _affinityQueues[workerIndex].Dequeue();
            }
            else if (_queue.Count > 0 && _queue.Peek().Priority == bestPriority) {
                task = _queue.Dequeue();
            }
            else {
                task = null!;
                for (int i = 0; i < _affinityQueues.Length; i++) {
                    if (_affinityQueues[i].Count > 0 &&
                        _affinityQueues[i].Peek().Priority == bestPriority) {
                        task = _affinityQueues[i].Dequeue();
                        break;
                    }
                }
            }

            _count--;
            return true;
        }
    }

    public void CompleteAdding() {
        lock (_lock) {
            _addingCompleted = true;
            Monitor.PulseAll(_lock);
        }
    }
}
