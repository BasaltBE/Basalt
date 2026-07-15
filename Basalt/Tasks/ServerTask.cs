namespace Basalt.Core.Tasks;

public abstract class ServerTask
{
    public bool RunOnMainThread { get; init; }
    internal bool IsExecuted { get; set; }
    internal bool IsCompleted { get; set; }
    public bool IsCancelled { get; private set; }
    internal int OwnerThreadId { get; set; }

    public abstract void Execute();

    public virtual void Complete() { }

    public void Cancel()
    {
        IsCancelled = true;
    }
}
