namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Profiling;
using Basalt.Core.Tasks;
using Basalt.Core.Worlds.Dimensions;
using BedrockProtocol.Types;

internal sealed class MobSpawnerTickTask : DelayedTask {
    private readonly Dimension _dimension;
    private readonly BlockPos _position;

    public MobSpawnerTickTask(Dimension dimension, BlockPos position) {
        _dimension = dimension;
        _position = position;
        DelayTicks = 1;
        RunOnMainThread = true;
    }

    public override void Execute() {
        using var _ = Profiler.Enabled ? Profiler.BeginZone("MobSpawner.Tick") : default;
        if (!_dimension.ChunkLoaded(_position.X >> 4, _position.Z >> 4)) {
            return;
        }

        Block? block = _dimension.GetBlock(_position.X, _position.Y, _position.Z);
        MobSpawnerTrait? trait = block?.GetTrait<MobSpawnerTrait>();

        if (trait is null) {
            return;
        }

        trait.Tick(_dimension, _position);
        _dimension.World?.Scheduler?.Schedule(new MobSpawnerTickTask(_dimension, _position));
    }
}
