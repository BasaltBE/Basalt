namespace Basalt.Core.Tasks;

using System.Threading.Channels;

public sealed class ExecutionDomainMailbox {
    private readonly Channel<MailboxCommand> _commands;
    private readonly Dictionary<object, CoalescedCommand> _coalescedCommands = [];
    private readonly Lock _coalescedLock = new();
    private int _pendingCount;
    private int _completed;

    public ExecutionDomainMailbox(int capacity) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _commands = Channel.CreateBounded<MailboxCommand>(new BoundedChannelOptions(capacity) {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public int PendingCount => Volatile.Read(ref _pendingCount);
    public bool IsCompleted => Volatile.Read(ref _completed) != 0;

    public bool TryEnqueue(Action command) {
        return TryEnqueue(command, null);
    }

    internal bool TryEnqueue(Action command, Action? cancellation) {
        ArgumentNullException.ThrowIfNull(command);
        if (IsCompleted) {
            return false;
        }

        Interlocked.Increment(ref _pendingCount);
        if (!_commands.Writer.TryWrite(new MailboxCommand(command, cancellation))) {
            Interlocked.Decrement(ref _pendingCount);
            return false;
        }

        return true;
    }

    internal bool TryEnqueueCoalesced(object key, Action command, Action? cancellation = null) {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(command);

        lock (_coalescedLock) {
            if (IsCompleted) {
                return false;
            }

            if (_coalescedCommands.TryGetValue(key, out CoalescedCommand? existing)) {
                existing.Command = command;
                existing.Cancellation = cancellation;
                return true;
            }

            _coalescedCommands[key] = new CoalescedCommand(command, cancellation);
            Interlocked.Increment(ref _pendingCount);
            if (_commands.Writer.TryWrite(new MailboxCommand(
                    () => ExecuteCoalesced(key),
                    () => CancelCoalesced(key)))) {
                return true;
            }

            _coalescedCommands.Remove(key);
            Interlocked.Decrement(ref _pendingCount);
            return false;
        }
    }

    public int Drain(int limit, Action<Exception> reportFailure) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        ArgumentNullException.ThrowIfNull(reportFailure);

        int drained = 0;
        while (drained < limit && _commands.Reader.TryRead(out MailboxCommand command)) {
            Interlocked.Decrement(ref _pendingCount);
            try {
                command.Execute();
            }
            catch (Exception exception) {
                reportFailure(exception);
            }

            drained++;
        }

        return drained;
    }

    public void Complete() {
        lock (_coalescedLock) {
            if (Interlocked.Exchange(ref _completed, 1) != 0) {
                return;
            }

            _commands.Writer.TryComplete();
        }

        while (_commands.Reader.TryRead(out MailboxCommand command)) {
            Interlocked.Decrement(ref _pendingCount);
            try {
                command.Cancellation?.Invoke();
            }
            catch (Exception exception) {
                Logger.Warn($"Mailbox cancellation failed: {exception}");
            }
        }

        lock (_coalescedLock) {
            foreach (CoalescedCommand command in _coalescedCommands.Values) {
                try {
                    command.Cancellation?.Invoke();
                }
                catch (Exception exception) {
                    Logger.Warn($"Mailbox cancellation failed: {exception}");
                }

                Interlocked.Decrement(ref _pendingCount);
            }

            _coalescedCommands.Clear();
        }
    }

    private void ExecuteCoalesced(object key) {
        CoalescedCommand? command;
        lock (_coalescedLock) {
            _coalescedCommands.Remove(key, out command);
        }

        command?.Command();
    }

    private void CancelCoalesced(object key) {
        CoalescedCommand? command;
        lock (_coalescedLock) {
            _coalescedCommands.Remove(key, out command);
        }

        command?.Cancellation?.Invoke();
    }

    private readonly record struct MailboxCommand(Action Execute, Action? Cancellation);

    private sealed class CoalescedCommand(Action command, Action? cancellation) {
        public Action Command { get; set; } = command;
        public Action? Cancellation { get; set; } = cancellation;
    }
}
