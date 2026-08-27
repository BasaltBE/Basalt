namespace Basalt.Tests;

using Basalt.Core.Tasks;

public sealed class TaskWorkerPoolTests {
    [Fact]
    public void QueueUsesPreferredWorkerBeforeStealing() {
        PriorityTaskQueue queue = new(4, 2);
        AffinityTask task = new() { WorkerAffinity = 1 };

        Assert.True(queue.TryAdd(task));
        Assert.True(queue.TryTake(1, out ServerTask? selected));
        Assert.Same(task, selected);
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void WorkerPoolAssignsAffinityInsideTheScheduler() {
        using TaskWorkerPool pool = new(2);
        ExecutionDomainMailbox mailbox = new(1);
        using ManualResetEventSlim executed = new();
        CountingTask task = new(executed) {
            CompletionMailbox = mailbox
        };

        Assert.Equal(-1, task.WorkerAffinity);
        Assert.True(pool.TryEnqueue(task));
        Assert.InRange(task.WorkerAffinity, 0, pool.WorkerCount - 1);
    }

    [Fact]
    public void QueueStealsAffinityWorkWhenPreferredWorkerIsIdle() {
        PriorityTaskQueue queue = new(4, 2);
        AffinityTask task = new() { WorkerAffinity = 0 };

        Assert.True(queue.TryAdd(task));
        Assert.True(queue.TryTake(1, out ServerTask? selected));
        Assert.Same(task, selected);
    }

    [Fact]
    public void QueueKeepsHighPriorityAheadOfAffinity() {
        PriorityTaskQueue queue = new(4, 2);
        AffinityTask low = new() {
            WorkerAffinity = 1,
            Priority = TaskPriority.Low
        };
        AffinityTask high = new() {
            Priority = TaskPriority.High
        };

        Assert.True(queue.TryAdd(low));
        Assert.True(queue.TryAdd(high));
        Assert.True(queue.TryTake(1, out ServerTask? selected));
        Assert.Same(high, selected);
    }

    [Fact]
    public void QueuedTasksCompleteAndRecordQueueWait() {
        using TaskWorkerPool pool = new(1);
        using ManualResetEventSlim completed = new();
        CountingTask task = new(completed);
        task.MainThreadCompletion = false;

        Assert.True(pool.TryEnqueue(task));

        Assert.True(completed.Wait(TimeSpan.FromSeconds(5)));
        Assert.True(SpinWait.SpinUntil(() => task.IsCompleted, TimeSpan.FromSeconds(5)));
        Assert.True(task.IsCompleted);
        Assert.True(pool.AverageQueueWaitMilliseconds >= 0);
    }

    [Fact]
    public void BackgroundTaskDoesNotUseMainThreadCompletionByDefault() {
        using TaskWorkerPool pool = new(1);
        using ManualResetEventSlim completed = new();
        CountingTask task = new(completed);

        Assert.True(pool.TryEnqueue(task));
        Assert.True(completed.Wait(TimeSpan.FromSeconds(5)));
        Assert.True(SpinWait.SpinUntil(() => task.IsCompleted, TimeSpan.FromSeconds(5)));
        Assert.False(task.MainThreadCompletion);
        Assert.True(task.IsCompleted);
    }

    [Fact]
    public void FullQueueDoesNotBlockAdmission() {
        using TaskWorkerPool pool = new(1);
        using ManualResetEventSlim started = new();
        using ManualResetEventSlim release = new();
        BlockingTask blocking = new(started, release) { MainThreadCompletion = false };

        Assert.True(pool.TryEnqueue(blocking));
        Assert.True(started.Wait(TimeSpan.FromSeconds(5)));

        for (int i = 0; i < 256; i++) {
            NoOpTask task = new() { MainThreadCompletion = false };
            Assert.True(pool.TryEnqueue(task));
        }

        NoOpTask rejected = new() { MainThreadCompletion = false };
        Assert.False(pool.TryEnqueue(rejected));
        release.Set();
    }

    [Fact]
    public void OwnerMailboxReceivesTaskCompletion() {
        using TaskWorkerPool pool = new(1);
        using ManualResetEventSlim executed = new();
        using ManualResetEventSlim completed = new();
        ExecutionDomainMailbox mailbox = new(4);
        CompletionTask task = new(executed, completed) {
            CompletionMailbox = mailbox
        };

        Assert.True(pool.TryEnqueue(task));
        Assert.True(executed.Wait(TimeSpan.FromSeconds(5)));
        Assert.False(completed.IsSet);

        Assert.True(SpinWait.SpinUntil(() => mailbox.PendingCount == 1, TimeSpan.FromSeconds(5)));
        Assert.Equal(1, mailbox.Drain(1, _ => Assert.Fail("The completion did not fail.")));
        Assert.True(completed.IsSet);
        Assert.True(task.IsCompleted);
    }

    [Fact]
    public void BackgroundWorkRunsBeforeOwnerCompletion() {
        using TaskWorkerPool pool = new(1);
        using ManualResetEventSlim executed = new();
        using ManualResetEventSlim completed = new();
        ExecutionDomainMailbox mailbox = new(1);
        BackgroundTask task = new(executed, completed) {
            CompletionMailbox = mailbox
        };

        Assert.True(pool.TryEnqueue(task));
        Assert.True(executed.Wait(TimeSpan.FromSeconds(5)));
        Assert.True(task.WorkerExecution);
        Assert.False(completed.IsSet);

        Assert.True(SpinWait.SpinUntil(() => mailbox.PendingCount == 1, TimeSpan.FromSeconds(5)));
        Assert.Equal(1, mailbox.Drain(1, _ => Assert.Fail("The owner completion failed.")));
        Assert.True(completed.IsSet);
    }

    [Fact]
    public void ClosedOwnerMailboxCancelsTaskCompletion() {
        using TaskWorkerPool pool = new(1);
        using ManualResetEventSlim executed = new();
        using ManualResetEventSlim completed = new();
        ExecutionDomainMailbox mailbox = new(1);
        CompletionTask task = new(executed, completed) {
            CompletionMailbox = mailbox
        };

        Assert.True(pool.TryEnqueue(task));
        Assert.True(executed.Wait(TimeSpan.FromSeconds(5)));
        mailbox.Complete();

        Assert.True(SpinWait.SpinUntil(() => task.IsCompleted, TimeSpan.FromSeconds(5)));
        Assert.False(completed.IsSet);
        Assert.True(task.IsCancelled);
    }

    [Fact]
    public void WorkerPoolRunsHighPriorityWorkFirst() {
        using TaskWorkerPool pool = new(1);
        using ManualResetEventSlim started = new();
        using ManualResetEventSlim release = new();
        using ManualResetEventSlim completed = new();
        BlockingTask blocking = new(started, release) { MainThreadCompletion = false };
        List<int> order = [];

        Assert.True(pool.TryEnqueue(blocking));
        Assert.True(started.Wait(TimeSpan.FromSeconds(5)));
        Assert.True(pool.TryEnqueue(new OrderedTask(order, completed, 3) {
            MainThreadCompletion = false,
            Priority = TaskPriority.Low
        }));
        Assert.True(pool.TryEnqueue(new OrderedTask(order, completed, 1) {
            MainThreadCompletion = false,
            Priority = TaskPriority.High
        }));
        Assert.True(pool.TryEnqueue(new OrderedTask(order, completed, 2) {
            MainThreadCompletion = false,
            Priority = TaskPriority.Normal
        }));

        release.Set();

        Assert.True(completed.Wait(TimeSpan.FromSeconds(5)));
        Assert.Equal([1, 2, 3], order);
    }

    [Fact]
    public void FailedTaskDoesNotRunCompletion() {
        using TaskWorkerPool pool = new(1);
        using ManualResetEventSlim executed = new();
        FailingTask task = new(executed) { MainThreadCompletion = false };

        Assert.True(pool.TryEnqueue(task));
        Assert.True(executed.Wait(TimeSpan.FromSeconds(5)));
        Assert.True(SpinWait.SpinUntil(() => task.IsCompleted, TimeSpan.FromSeconds(5)));
        Assert.True(task.ExecutionFailed);
        Assert.False(task.CompletionRan);
    }

    private sealed class CountingTask : ServerTask {
        private readonly ManualResetEventSlim _completed;

        public CountingTask(ManualResetEventSlim completed) {
            _completed = completed;
        }

        public override void Execute() {
            _completed.Set();
        }
    }

    private sealed class BlockingTask : ServerTask {
        private readonly ManualResetEventSlim _started;
        private readonly ManualResetEventSlim _release;

        public BlockingTask(ManualResetEventSlim started, ManualResetEventSlim release) {
            _started = started;
            _release = release;
        }

        public override void Execute() {
            _started.Set();
            _release.Wait(TimeSpan.FromSeconds(5));
        }
    }

    private sealed class NoOpTask : ServerTask {
        public override void Execute() { }
    }

    private sealed class CompletionTask : ServerTask {
        private readonly ManualResetEventSlim _executed;
        private readonly ManualResetEventSlim _completed;

        public CompletionTask(ManualResetEventSlim executed, ManualResetEventSlim completed) {
            _executed = executed;
            _completed = completed;
        }

        public override void Execute() {
            _executed.Set();
        }

        public override void Complete() {
            _completed.Set();
        }
    }

    private sealed class BackgroundTask : ServerTask {
        private readonly ManualResetEventSlim _executed;
        private readonly ManualResetEventSlim _completed;

        public BackgroundTask(ManualResetEventSlim executed, ManualResetEventSlim completed) {
            _executed = executed;
            _completed = completed;
        }

        public bool WorkerExecution { get; private set; }

        public override void Execute() {
            WorkerExecution = TaskWorkerPool.WorkerThread;
            _executed.Set();
        }

        public override void Complete() {
            _completed.Set();
        }
    }

    private sealed class OrderedTask : ServerTask {
        private readonly List<int> _order;
        private readonly ManualResetEventSlim _completed;
        private readonly int _value;

        public OrderedTask(List<int> order, ManualResetEventSlim completed, int value) {
            _order = order;
            _completed = completed;
            _value = value;
        }

        public override void Execute() {
            _order.Add(_value);
            if (_order.Count == 3) {
                _completed.Set();
            }
        }
    }

    private sealed class FailingTask : ServerTask {
        private readonly ManualResetEventSlim _executed;

        public FailingTask(ManualResetEventSlim executed) {
            _executed = executed;
        }

        public bool CompletionRan { get; private set; }

        public override void Execute() {
            _executed.Set();
            throw new InvalidOperationException("Task execution failed.");
        }

        public override void Complete() {
            CompletionRan = true;
        }
    }

    private sealed class AffinityTask : ServerTask {
        public override void Execute() { }
    }
}
