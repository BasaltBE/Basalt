namespace Basalt.Tests;

using Basalt.Core.Tasks;
using Basalt.Core.Worlds;

public sealed class WorldSchedulerTests {
    [Fact]
    public void OwnerExecutionRunsInsideTheMailbox() {
        using World world = new("test");
        using TaskWorkerPool workerPool = new(1);
        WorldScheduler scheduler = new(world, workerPool);
        ExecutionDomainMailbox mailbox = new(2);
        using ManualResetEventSlim executed = new();

        OwnerTask task = new(executed) {
            RunOnMainThread = true,
            ExecutionMailbox = mailbox
        };
        scheduler.Schedule(task);
        scheduler.Tick();

        Assert.False(executed.IsSet);
        Assert.Equal(1, mailbox.Drain(1, _ => Assert.Fail("The command did not fail.")));
        Assert.True(executed.IsSet);
    }

    [Fact]
    public void ClosedOwnerMailboxDiscardsDeferredTask() {
        using World world = new("test");
        using TaskWorkerPool workerPool = new(1);
        WorldScheduler scheduler = new(world, workerPool);
        ExecutionDomainMailbox mailbox = new(1);
        mailbox.TryEnqueue(static () => { });
        OwnerTask task = new(new ManualResetEventSlim()) {
            ExecutionMailbox = mailbox
        };

        scheduler.Schedule(task);
        scheduler.Tick();
        mailbox.Complete();
        scheduler.Tick();

        Assert.True(task.IsCancelled);
        Assert.True(task.IsCompleted);
    }

    [Fact]
    public void DeferredDomainQueueRejectsWorkAtCapacity() {
        using World world = new("test");
        using TaskWorkerPool workerPool = new(1);
        WorldScheduler scheduler = new(world, workerPool);
        ExecutionDomainMailbox mailbox = new(1);
        mailbox.TryEnqueue(static () => { });

        DomainTask[] tasks = new DomainTask[4097];
        for (int i = 0; i < tasks.Length; i++) {
            tasks[i] = new DomainTask(mailbox);
            scheduler.Schedule(tasks[i]);
        }

        scheduler.Tick();

        Assert.Equal(4096, scheduler.PendingDeferredDomainWorkCount);
        Assert.True(tasks[^1].IsCancelled);
    }

    private sealed class OwnerTask : ServerTask {
        private readonly ManualResetEventSlim _executed;

        public OwnerTask(ManualResetEventSlim executed) {
            _executed = executed;
        }

        public override void Execute() {
            _executed.Set();
        }
    }

    private sealed class DomainTask : ServerTask {
        public DomainTask(ExecutionDomainMailbox mailbox) {
            ExecutionMailbox = mailbox;
        }

        public override void Execute() { }
    }
}
