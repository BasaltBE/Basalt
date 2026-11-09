using BasaltBE.Basalt.Worlds.Providers.Types;

namespace BasaltBE.Basalt.Worlds.Providers;

public interface IEntityProvider {
    long ReserveEntityIds(long count);

    EntityStorageRecord[] ReadEntities(ChunkStorageKey key);

    void WriteEntities(ReadOnlySpan<EntityStorageChange> changes);
}
