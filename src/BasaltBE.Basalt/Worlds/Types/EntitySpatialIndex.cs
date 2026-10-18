using System.Runtime.InteropServices;
using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Worlds.Types;

internal sealed class EntitySpatialIndex
{
    private readonly Dictionary<long, List<Entity>> chunks = new(ChunkKeyComparer.Instance);
    internal bool Deferred;
    private readonly List<Entity> changed = [];

    internal void Flush()
    {
        Deferred = false;
        foreach (Entity entity in changed)
        {
            entity.SpatialDirty = false;
            if (entity.SpawnedDimension is null)
                Remove(entity);
            else
                Update(entity);
        }
        changed.Clear();
    }

    internal void Remove(Entity entity)
    {
        if (Deferred)
        {
            if (!entity.SpatialDirty)
            {
                entity.SpatialDirty = true;
                changed.Add(entity);
            }
            return;
        }
        if (entity.SpatialSlot < 0)
            return;
        List<Entity> bucket = chunks[entity.SpatialChunk];
        Entity last = bucket[^1];
        bucket[entity.SpatialSlot] = last;
        last.SpatialSlot = entity.SpatialSlot;
        bucket.RemoveAt(bucket.Count - 1);
        if (bucket.Count == 0)
            chunks.Remove(entity.SpatialChunk);
        entity.SpatialSlot = -1;
    }

    internal void Update(Entity entity)
    {
        if (Deferred)
        {
            if (!entity.SpatialDirty)
            {
                entity.SpatialDirty = true;
                changed.Add(entity);
            }
            return;
        }
        if (!float.IsFinite(entity.Position.X) || !float.IsFinite(entity.Position.Z))
        {
            Remove(entity);
            return;
        }
        int x = (int)MathF.Floor(entity.Position.X) >> 4;
        int z = (int)MathF.Floor(entity.Position.Z) >> 4;
        long key = ((long)x << 32) | (uint)z;
        if (entity.SpatialSlot >= 0 && entity.SpatialChunk == key)
            return;
        Remove(entity);
        if (!chunks.TryGetValue(key, out List<Entity>? bucket))
        {
            bucket = [];
            chunks.Add(key, bucket);
        }
        entity.SpatialChunk = key;
        entity.SpatialSlot = bucket.Count;
        bucket.Add(entity);
    }

    internal void Update(ReadOnlySpan<Entity> entities)
    {
        foreach (Entity entity in entities)
            Update(entity);
    }

    internal void Collect(HashSet<long> loadedChunks, List<Entity> destination)
    {
        destination.Clear();
        foreach (long key in loadedChunks)
        {
            if (chunks.TryGetValue(key, out List<Entity>? bucket))
                destination.AddRange(bucket);
        }
    }

    internal ReadOnlySpan<Entity> GetChunkEntities(int chunkX, int chunkZ)
    {
        long key = ((long)chunkX << 32) | (uint)chunkZ;
        return chunks.TryGetValue(key, out List<Entity>? bucket)
            ? CollectionsMarshal.AsSpan(bucket)
            : [];
    }
}
