namespace Basalt.Core.Tasks;

public abstract class DelayedTask : ServerTask {
    public uint DelayTicks { get; init; }
    internal ulong ExecutionTick { get; set; }
}
