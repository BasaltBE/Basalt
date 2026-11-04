using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Features;

public sealed class FeatureContext {
    public readonly Dimension Dimension;
    public readonly Random Random;
    readonly int capacity;
    internal readonly Dictionary<(int X, int Y, int Z), BlockPermutation> Blocks = [];
    internal bool Invalid;
    internal Dictionary<(int X, int Y, int Z), CompoundTag>? BlockActors;
    internal List<Entity>? Entities;

    public void AddEntity(Entity entity) {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity is Player || entity.Type is null || entity.SpawnedDimension is not null)
            throw new ArgumentException("A feature requires a new non-player entity with a type.", nameof(entity));
        if (!float.IsFinite(entity.Position.X) || !float.IsFinite(entity.Position.Y) || !float.IsFinite(entity.Position.Z) ||
            entity.Position.Y < Dimension.MinimumY || entity.Position.Y >= (long)Dimension.MaximumY + 1) {
            Invalid = true;
            return;
        }
        Entities ??= [];
        Entities.Add(entity);
    }

    internal FeatureContext(Dimension dimension, Random random, int capacity) {
        Dimension = dimension;
        Random = random;
        this.capacity = capacity;
    }

    public BlockPermutation GetBlock(int x, int y, int z) => Blocks.GetValueOrDefault((x, y, z)) ?? Dimension.GetPermutation(x, y, z);

    public void SetBlockNbt(int x, int y, int z, CompoundTag data) {
        ArgumentNullException.ThrowIfNull(data);
        if (!Blocks.ContainsKey((x, y, z)))
            throw new InvalidOperationException("Add the block before its block actor data.");
        BlockActors ??= [];
        BlockActors[(x, y, z)] = data;
    }

    public bool SetBlock(int x, int y, int z, BlockPermutation permutation) {
        ArgumentNullException.ThrowIfNull(permutation);
        if (y < Dimension.MinimumY || y > Dimension.MaximumY ||
            Blocks.Count >= capacity && !Blocks.ContainsKey((x, y, z))) {
            Invalid = true;
            return false;
        }
        Blocks[(x, y, z)] = permutation;
        return true;
    }
}
