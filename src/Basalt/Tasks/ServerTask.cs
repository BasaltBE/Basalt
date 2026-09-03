namespace Basalt.Core.Tasks;

using Basalt.Core.Plugins;

public abstract class ServerTask {
    public bool RunOnMainThread { get; init; }
    public TaskPriority Priority { get; init; } = TaskPriority.Normal;
    internal bool IsExecuted { get; set; }
    internal bool IsCompleted { get; set; }
    internal bool ExecutionFailed { get; set; }
    internal bool MainThreadCompletion;
    internal int WorkerAffinity { get; set; } = -1;
    internal ExecutionDomainMailbox? ExecutionMailbox { get; set; }
    internal ExecutionDomainMailbox? CompletionMailbox { get; set; }
    public bool IsCancelled { get; private set; }
    internal long QueuedTimestamp;
    internal ServerTask? NextInSlot { get; set; }
    internal PluginContainer? Owner { get; set; }
    internal Action<PluginContainer?, string, Exception>? RuntimeErrorHandler { get; set; }
    internal Action<ServerTask>? CompletionHandler { get; set; }
    private int _completionNotified;

    public abstract void Execute();

    public virtual void Complete() { }

    public virtual void OnStop() { }

    internal void ExecuteOnDomain() {
        bool succeeded = true;
        try {
            Execute();
        }
        catch (Exception exception) {
            succeeded = false;
            ExecutionFailed = true;
            ReportFailure("task execution", exception);
        }

        IsExecuted = true;
        if (succeeded) {
            try {
                Complete();
            }
            catch (Exception exception) {
                ReportFailure("task completion", exception);
            }
        }

        MarkCompleted();
    }

    public void Cancel() {
        IsCancelled = true;
        MarkCompleted();
    }

    internal void Stop() {
        try {
            OnStop();
        }
        catch (Exception exception) {
            ReportFailure("task stop", exception);
        }
    }

    internal void ReportFailure(string callback, Exception exception) {
        if (RuntimeErrorHandler is { } handler)
            handler(Owner, $"{GetType().Name} {callback}", exception);
        else
            Logger.Warn($"{GetType().Name} {callback} failed: {exception}");
    }

    internal void MarkCompleted() {
        IsCompleted = true;
        if (Interlocked.Exchange(ref _completionNotified, 1) == 0)
            CompletionHandler?.Invoke(this);
    }

    internal void Reset() {
        IsCancelled = false;
        IsExecuted = false;
        IsCompleted = false;
        ExecutionFailed = false;
        _completionNotified = 0;
        WorkerAffinity = -1;
        NextInSlot = null;
    }
}
