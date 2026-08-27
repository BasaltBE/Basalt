namespace Basalt.Tests;

using Basalt.Core.Tasks;

public sealed class ExecutionDomainMailboxTests {
    [Fact]
    public void EnqueueRejectsCommandsWhenMailboxIsFull() {
        ExecutionDomainMailbox mailbox = new(1);

        Assert.True(mailbox.TryEnqueue(static () => { }));
        Assert.False(mailbox.TryEnqueue(static () => { }));
        Assert.Equal(1, mailbox.PendingCount);
    }

    [Fact]
    public void DrainRunsCommandsInQueueOrder() {
        ExecutionDomainMailbox mailbox = new(4);
        List<int> values = [];

        mailbox.TryEnqueue(() => values.Add(1));
        mailbox.TryEnqueue(() => values.Add(2));
        mailbox.TryEnqueue(() => values.Add(3));

        int drained = mailbox.Drain(3, _ => Assert.Fail("The command did not fail."));

        Assert.Equal(3, drained);
        Assert.Equal([1, 2, 3], values);
        Assert.Equal(0, mailbox.PendingCount);
    }

    [Fact]
    public void DrainLimitLeavesRemainingCommandsQueued() {
        ExecutionDomainMailbox mailbox = new(4);
        int value = 0;

        mailbox.TryEnqueue(() => value++);
        mailbox.TryEnqueue(() => value++);
        mailbox.TryEnqueue(() => value++);

        Assert.Equal(2, mailbox.Drain(2, _ => Assert.Fail("The command did not fail.")));
        Assert.Equal(2, value);
        Assert.Equal(1, mailbox.PendingCount);
    }

    [Fact]
    public void CoalescedCommandRunsTheLatestValue() {
        ExecutionDomainMailbox mailbox = new(4);
        object key = new();
        int value = 0;

        Assert.True(mailbox.TryEnqueueCoalesced(key, () => value = 1));
        Assert.True(mailbox.TryEnqueueCoalesced(key, () => value = 2));
        Assert.True(mailbox.TryEnqueueCoalesced(key, () => value = 3));

        Assert.Equal(1, mailbox.PendingCount);
        Assert.Equal(1, mailbox.Drain(1, _ => Assert.Fail("The command did not fail.")));
        Assert.Equal(3, value);
        Assert.Equal(0, mailbox.PendingCount);
    }

    [Fact]
    public void CompleteCancelsCoalescedCommand() {
        ExecutionDomainMailbox mailbox = new(1);
        object key = new();
        bool cancelled = false;

        Assert.True(mailbox.TryEnqueueCoalesced(key, static () => { }, () => cancelled = true));
        mailbox.Complete();

        Assert.True(cancelled);
        Assert.Equal(0, mailbox.PendingCount);
    }

    [Fact]
    public void CommandFailureDoesNotStopTheDrain() {
        ExecutionDomainMailbox mailbox = new(2);
        List<Exception> failures = [];
        int completed = 0;

        mailbox.TryEnqueue(static () => throw new InvalidOperationException("Command failed."));
        mailbox.TryEnqueue(() => completed++);

        Assert.Equal(2, mailbox.Drain(2, failures.Add));
        Assert.Single(failures);
        Assert.Equal(1, completed);
    }

    [Fact]
    public void CompleteRejectsNewCommands() {
        ExecutionDomainMailbox mailbox = new(1);
        mailbox.Complete();

        Assert.False(mailbox.TryEnqueue(static () => { }));
        Assert.True(mailbox.IsCompleted);
        Assert.Equal(0, mailbox.PendingCount);
    }

    [Fact]
    public void CompleteCancelsQueuedCommands() {
        ExecutionDomainMailbox mailbox = new(1);
        bool cancelled = false;

        Assert.True(mailbox.TryEnqueue(static () => { }, () => cancelled = true));
        mailbox.Complete();

        Assert.True(cancelled);
        Assert.Equal(0, mailbox.PendingCount);
    }

    [Fact]
    public async Task ConcurrentEnqueueAndDrainKeepsPendingCountNonNegative() {
        ExecutionDomainMailbox mailbox = new(256);
        int minimum = mailbox.PendingCount;

        Task producer = Task.Run(() => {
            for (int index = 0; index < 10000; index++) {
                mailbox.TryEnqueue(static () => { });
            }
        });

        while (!producer.IsCompleted) {
            mailbox.Drain(64, _ => Assert.Fail("The command did not fail."));
            minimum = Math.Min(minimum, mailbox.PendingCount);
            Thread.Yield();
        }

        await producer;
        while (mailbox.PendingCount > 0) {
            mailbox.Drain(64, _ => Assert.Fail("The command did not fail."));
            minimum = Math.Min(minimum, mailbox.PendingCount);
        }

        Assert.True(minimum >= 0);
    }
}
