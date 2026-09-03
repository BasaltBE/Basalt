namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Tasks;
using Basalt.Core.Profiling;
using Basalt.Core.Worlds.Dimensions;
using Basalt.BedrockProtocol.Types;

internal sealed class HopperTickTask : DelayedTask {
    private readonly Dimension _dimension;
    private readonly BlockPos _position;

    public HopperTickTask(Dimension dimension, BlockPos position) {
        _dimension = dimension;
        _position = position;
        DelayTicks = 1;
        RunOnMainThread = true;
        ExecutionMailbox = dimension.Mailbox;
    }

    public override void Execute() {
        using var _ = Profiler.Enabled ? Profiler.BeginZone("Hopper.Tick") : default;
        Block? block = _dimension.GetBlock(_position.X, _position.Y, _position.Z);
        HopperTrait? trait = block?.GetTrait<HopperTrait>();

        if (trait is null) {
            // Chunk was unloaded or block removed. The trait will reschedule
            // itself when the chunk reloads and EnsureContainer runs.
            return;
        }

        bool shouldContinue = trait.Tick();
        if (!shouldContinue) {
            trait.MarkTickingStopped();
            return;
        }

        _dimension.World?.Scheduler?.Schedule(new HopperTickTask(_dimension, _position));
    }

    public override void OnStop() {
        Block? block = _dimension.GetBlock(_position.X, _position.Y, _position.Z);
        block?.GetTrait<HopperTrait>()?.MarkTickingStopped();
    }
}
