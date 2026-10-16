using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Worlds.Types;

internal sealed class EntitySpatialIndex
{
    private readonly Dictionary<long, List<Entity>> chunks = [];
    private readonly List<long> emptyChunks = [];
    private readonly Stack<List<Entity>> spareBuckets = [];
    internal bool Ready;

    internal void Update(ReadOnlySpan<Entity> entities)
    {
        foreach (List<Entity> bucket in chunks.Values)
            bucket.Clear();
        foreach (Entity entity in entities)
        {
            if (!float.IsFinite(entity.Position.X) || !float.IsFinite(entity.Position.Z))
                continue;
            int x = (int)MathF.Floor(entity.Position.X) >> 4;
            int z = (int)MathF.Floor(entity.Position.Z) >> 4;
            long key = ((long)x << 32) | (uint)z;
            if (!chunks.TryGetValue(key, out List<Entity>? bucket))
            {
                bucket = spareBuckets.Count == 0 ? [] : spareBuckets.Pop();
                chunks.Add(key, bucket);
            }
            bucket.Add(entity);
        }
        emptyChunks.Clear();
        foreach ((long key, List<Entity> bucket) in chunks)
        {
            if (bucket.Count == 0)
                emptyChunks.Add(key);
        }
        foreach (long key in emptyChunks)
        {
            spareBuckets.Push(chunks[key]);
            chunks.Remove(key);
        }
        Ready = true;
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
}
