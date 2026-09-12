namespace Basalt.Core.Entities.AI.Goal;

using Basalt.Core.Entities.AI;

public abstract class Goal {
    public GoalControl Controls { get; }
    public int Priority { get; }
    public bool Running { get; internal set; }
    public bool Interruptible { get; }
    public ulong UpdateInterval { get; }

    protected Goal(
        int priority,
        GoalControl controls,
        bool interruptible = true,
        ulong updateInterval = 1) {
        ArgumentOutOfRangeException.ThrowIfNegative(priority);
        ArgumentOutOfRangeException.ThrowIfLessThan(updateInterval, 1UL);
        Priority = priority;
        Controls = controls;
        Interruptible = interruptible;
        UpdateInterval = updateInterval;
    }

    public abstract bool CanUse(AiContext context);

    public virtual bool CanContinue(AiContext context) => CanUse(context);

    public virtual void Start(AiContext context) {
    }

    public virtual void Stop(AiContext context) {
    }

    public virtual void Tick(AiContext context) {
    }
}
