namespace Basalt.Core.Tasks;

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
            Logger.Warn($"Domain task execution failed: {exception}");
        }

        IsExecuted = true;
        if (succeeded) {
            try {
                Complete();
            }
            catch (Exception exception) {
                Logger.Warn($"Domain task completion failed: {exception}");
            }
        }

        IsCompleted = true;
    }

    public void Cancel() {
        IsCancelled = true;
        IsCompleted = true;
    }

    internal void Reset() {
        IsCancelled = false;
        IsExecuted = false;
        IsCompleted = false;
        ExecutionFailed = false;
        WorkerAffinity = -1;
        NextInSlot = null;
    }
}
