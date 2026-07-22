namespace Basalt.Core.Tasks;

public abstract class ServerTask {
  public bool RunOnMainThread { get; init; }
  public TaskPriority Priority { get; init; } = TaskPriority.Normal;
  internal bool IsExecuted { get; set; }
  internal bool IsCompleted { get; set; }
  public bool IsCancelled { get; private set; }
  internal int OwnerThreadId { get; set; }
  internal ServerTask? NextInSlot { get; set; }

  public abstract void Execute();

  public virtual void Complete() { }

  public virtual void OnStop() { }

  public void Cancel() {
    IsCancelled = true;
  }

  internal void Reset() {
    IsCancelled = false;
    IsExecuted = false;
    IsCompleted = false;
    NextInSlot = null;
  }
}
