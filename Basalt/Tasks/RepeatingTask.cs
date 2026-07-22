namespace Basalt.Core.Tasks;

public abstract class RepeatingTask : ServerTask {
    public uint IntervalTicks { get; init; } = 20;
    internal ulong NextExecutionTick { get; set; }
    public bool IsActive => !IsCancelled;
}
