namespace Basalt.Core.Entities;

using BedrockProtocol.Nbt;

public static class EntityFactory {
    private static readonly Dictionary<string, Func<CompoundTag, Entity>> Factories = new(StringComparer.Ordinal);

    public static void Register(string identifier, Func<CompoundTag, Entity> factory) {
        Factories[identifier] = factory;
    }

    public static Entity? Create(string identifier, CompoundTag tag) {
        return Factories.TryGetValue(identifier, out Func<CompoundTag, Entity>? factory)
            ? factory(tag)
            : null;
    }
}
