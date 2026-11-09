using BasaltBE.Basalt.Blocks.BlockActors.Types;

namespace BasaltBE.Basalt.Blocks.BlockActors;

public sealed class BlockActorRegistry {
    readonly Dictionary<string, Func<BlockActorContext, BlockActor>> factories = new(StringComparer.Ordinal);
    readonly HashSet<string> ticking = new(StringComparer.Ordinal);
    bool frozen;

    public void Register(string blockIdentifier, Func<BlockActorContext, BlockActor> factory, bool tick = false) {
        if (frozen) throw new InvalidOperationException("Server is already running.");

        ArgumentException.ThrowIfNullOrWhiteSpace(blockIdentifier);
        ArgumentNullException.ThrowIfNull(factory);

        factories[blockIdentifier] = factory;
        if (tick) ticking.Add(blockIdentifier); else ticking.Remove(blockIdentifier);
    }

    public bool Unregister(string blockIdentifier) {
        if (frozen) throw new InvalidOperationException("Server is already running.");
        ArgumentException.ThrowIfNullOrWhiteSpace(blockIdentifier);

        ticking.Remove(blockIdentifier);
        return factories.Remove(blockIdentifier);
    }

    internal void Freeze() => frozen = true;

    internal BlockActor? Create(string blockIdentifier, BlockActorContext context) =>
        factories.TryGetValue(blockIdentifier, out Func<BlockActorContext, BlockActor>? factory)
            ? factory(context)
            : null;

    internal bool Ticks(string blockIdentifier) => ticking.Contains(blockIdentifier);
}
