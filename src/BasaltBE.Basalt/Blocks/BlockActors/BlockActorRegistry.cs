using BasaltBE.Basalt.Blocks.BlockActors.Types;

namespace BasaltBE.Basalt.Blocks.BlockActors;

public sealed class BlockActorRegistry {
    readonly Dictionary<string, Func<BlockActorContext, BlockActor>> factories = new(StringComparer.Ordinal);
    bool frozen;

    public void Register(string blockIdentifier, Func<BlockActorContext, BlockActor> factory) {
        if (frozen)
            throw new InvalidOperationException("Block actor registrations are closed after the server starts.");
        ArgumentException.ThrowIfNullOrWhiteSpace(blockIdentifier);
        ArgumentNullException.ThrowIfNull(factory);
        factories[blockIdentifier] = factory;
    }

    public bool Unregister(string blockIdentifier) {
        if (frozen)
            throw new InvalidOperationException("Block actor registrations are closed after the server starts.");
        ArgumentException.ThrowIfNullOrWhiteSpace(blockIdentifier);
        return factories.Remove(blockIdentifier);
    }

    internal void Freeze() => frozen = true;

    internal BlockActor? Create(string blockIdentifier, BlockActorContext context) =>
        factories.TryGetValue(blockIdentifier, out Func<BlockActorContext, BlockActor>? factory)
            ? factory(context)
            : null;
}
