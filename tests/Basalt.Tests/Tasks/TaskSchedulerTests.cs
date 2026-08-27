namespace Basalt.Tests;

using Basalt.Core.Tasks;

public sealed class TaskSchedulerTests {
    [Fact]
    public void DeferredDomainQueueRejectsWorkAtCapacity() {
        using TaskWorkerPool workerPool = new(1);
        TaskScheduler scheduler = new(workerPool);
        ExecutionDomainMailbox mailbox = new(1);
        mailbox.TryEnqueue(static () => { });

        DomainTask[] tasks = new DomainTask[4097];
        for (int i = 0; i < tasks.Length; i++) {
            tasks[i] = new DomainTask(mailbox);
            scheduler.Schedule(tasks[i]);
        }

        Assert.Equal(4096, scheduler.PendingDeferredDomainWorkCount);
        Assert.True(tasks[^1].IsCancelled);

        DomainTask rejected = new(mailbox);
        scheduler.Schedule(rejected);
        Assert.Equal(4096, scheduler.PendingDeferredDomainWorkCount);
        Assert.True(rejected.IsCancelled);
    }

    private sealed class DomainTask : ServerTask {
        public DomainTask(ExecutionDomainMailbox mailbox) {
            ExecutionMailbox = mailbox;
        }

        public override void Execute() { }
    }
}
