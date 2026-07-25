namespace Basalt.Core.Tasks;

using Basalt.Core.Profiling;
using Basalt.Core.Worlds.Dimensions.Generation;
using Basalt.Core.Worlds.Dimensions.Provider;
using Basalt.Protocol.Enums;
using ChunkColumn = Worlds.Dimensions.Chunk.Chunk;

public sealed class ChunkGenerationTask : ServerTask {
    private readonly WorldProvider _provider;
    private readonly Generator _generator;
    private readonly DimensionType _dimensionType;
    private readonly int _x;
    private readonly int _z;
    private readonly long _hash;
    private readonly Action<long, ChunkColumn?> _onComplete;

    public ChunkColumn? Result { get; private set; }

    public ChunkGenerationTask(WorldProvider provider, Generator generator, DimensionType dimensionType, int x, int z, long hash, Action<long, ChunkColumn?> onComplete) {
        _provider = provider;
        _generator = generator;
        _dimensionType = dimensionType;
        _x = x;
        _z = z;
        _hash = hash;
        _onComplete = onComplete;
    }

    public override void Execute() {
        using var _ = Profiler.Enabled ? Profiler.BeginZone("ChunkGen.Execute") : default;
        ChunkColumn? loaded = _provider.LoadChunk(_dimensionType, _x, _z);
        if (loaded is null) {
            loaded = _generator.Generate(_dimensionType, _x, _z);
            _generator.Populate(loaded);
            loaded.Dirty = true;
        }

        Result = loaded;
    }

    public override void Complete() {
        _onComplete(_hash, Result);
    }
}
