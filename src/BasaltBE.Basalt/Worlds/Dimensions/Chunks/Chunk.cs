using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Nbt.Types;
using NbtCodec = BasaltBE.Nbt.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.Worlds.Dimensions.Chunks;

public sealed class Chunk(int x, int z) {
    static readonly Options NetworkNbtOptions = new() { VarInt = true, StringLengthVarInt = true };

    Dictionary<int, SubChunk>? subChunks;
    Dictionary<(int X, int Y, int Z), CompoundTag>? blockActors;
    Dictionary<(int X, int Y, int Z), BlockActor>? actors;
    internal Dimension? Dimension;

    public readonly int X = x;
    public readonly int Z = z;
    public int SubChunkCount => subChunks?.Count ?? 0;

    internal int GetSubChunkSendCount(int minimumSubChunkY, int maximumSubChunkCount) {
        for (int i = maximumSubChunkCount - 1; i >= 0; i--) {
            if (GetSubChunk(checked(minimumSubChunkY + i)) is { Empty: false }) {
                return i + 1;
            }
        }

        return 0;
    }

    public bool Empty {
        get {
            if (blockActors is { Count: > 0 }) {
                return false;
            }

            if (subChunks is null) {
                return true;
            }

            foreach (SubChunk subChunk in subChunks.Values) {
                if (!subChunk.Empty) {
                    return false;
                }
            }

            return true;
        }
    }

    public SubChunk? GetSubChunk(int subChunkY) =>
        subChunks is not null && subChunks.TryGetValue(subChunkY, out SubChunk? subChunk) ? subChunk : null;

    public SubChunk GetOrCreateSubChunk(int subChunkY) {
        subChunks ??= [];
        if (subChunks.TryGetValue(subChunkY, out SubChunk? subChunk)) {
            return subChunk;
        }

        subChunk = new SubChunk { Index = checked((sbyte)subChunkY) };
        subChunks.Add(subChunkY, subChunk);
        return subChunk;
    }

    public BlockPermutation GetPermutation(int x, int y, int z, int layer = 0) {
        if ((uint)layer > 1) {
            throw new ArgumentOutOfRangeException(nameof(layer), "A subchunk supports two block layers.");
        }

        int cellIndex = BlockStorage.GetIndex(x, y & 15, z);
        return subChunks is not null && subChunks.TryGetValue(y >> 4, out SubChunk? subChunk)
            ? subChunk.GetPermutationAt(cellIndex, layer)
            : BlockStorage.Air;
    }

    public void SetPermutation(int x, int y, int z, BlockPermutation permutation, int layer = 0) {
        ArgumentNullException.ThrowIfNull(permutation);
        if ((uint)layer > 1) {
            throw new ArgumentOutOfRangeException(nameof(layer), "A subchunk supports two block layers.");
        }

        int cellIndex = BlockStorage.GetIndex(x, y & 15, z);
        int subChunkY = y >> 4;
        if (subChunks is null || !subChunks.TryGetValue(subChunkY, out SubChunk? subChunk)) {
            if (permutation == BlockStorage.Air) {
                return;
            }

            subChunk = GetOrCreateSubChunk(subChunkY);
        }

        subChunk.SetPermutationAt(cellIndex, permutation, layer);
    }

    public Block GetBlock(int x, int y, int z, int layer = 0) {
        BlockPermutation permutation = GetPermutation(x, y, z, layer);
        BlockType type = BlockPalette.FindType(permutation.Identifier)
            ?? throw new KeyNotFoundException($"Block type '{permutation.Identifier}' was not found.");

        return new Block(type, permutation) {
            Nbt = GetBlockNbt(x, y, z),
            Actor = Dimension?.GetBlockActor(this, x, y, z)
        };
    }

    public void SetBlock(int x, int y, int z, Block block, int layer = 0) {
        ArgumentNullException.ThrowIfNull(block);
        SetPermutation(x, y, z, block.Permutation, layer);
        SetBlockNbt(x, y, z, block.Actor?.Nbt ?? block.Nbt);
        SetRuntimeActor(x, y, z, block.Actor);
    }

    public CompoundTag? GetBlockNbt(int x, int y, int z) {
        BlockStorage.GetIndex(x, y & 15, z);
        return blockActors?.GetValueOrDefault((x, y, z));
    }

    public void SetBlockNbt(int x, int y, int z, CompoundTag? actor) {
        BlockStorage.GetIndex(x, y & 15, z);
        if (actor is null) {
            blockActors?.Remove((x, y, z));
            actors?.Remove((x, y, z));
            return;
        }

        actor.Name = string.Empty;
        SetCoordinate(actor, "x", checked(X * 16 + x));
        SetCoordinate(actor, "y", y);
        SetCoordinate(actor, "z", checked(Z * 16 + z));
        blockActors ??= [];
        blockActors[(x, y, z)] = actor;
    }

    internal BlockActor? GetRuntimeActor(int x, int y, int z) => actors?.GetValueOrDefault((x, y, z));

    internal void SetRuntimeActor(int x, int y, int z, BlockActor? actor) {
        if (actor is null) {
            actors?.Remove((x, y, z));
            return;
        }

        (actors ??= [])[(x, y, z)] = actor;
    }

    /// <summary>Serializes/Writes chunk data into a writer.</summary>
    /// <param name="minimumSubChunkY">The first signed subchunk index in the payload.</param>
    /// <param name="subChunkCount">The number of subchunks requested by the level chunk packet.</param>
    /// <param name="biomeCount">The number of biome storages required for the dimension height.</param>
    public void Serialize(ref BinaryWriter writer, int minimumSubChunkY, int subChunkCount, int biomeCount) {
        if (subChunkCount is < 0 or > 256 || biomeCount is < 0 or > 256) {
            throw new ArgumentOutOfRangeException(nameof(subChunkCount), "Chunk section counts must be from 0 to 256.");
        }

        for (int i = 0; i < subChunkCount; i++) {
            int subChunkY = checked(minimumSubChunkY + i);
            sbyte index = checked((sbyte)subChunkY);
            if (GetSubChunk(subChunkY) is { } subChunk) {
                subChunk.Serialize(ref writer, index);
            }
            else {
                writer.WriteUInt8(9);
                writer.WriteUInt8(0);
                writer.WriteInt8(index);
            }
        }

        for (int i = 0; i < biomeCount; i++) {
            int subChunkY = checked(minimumSubChunkY + i);
            if (GetSubChunk(subChunkY) is { } subChunk) {
                subChunk.Biomes.Serialize(ref writer);
            }
            else {
                BiomeStorage.SerializeDefault(ref writer);
            }
        }

        writer.WriteUInt8(0);
        if (blockActors is null) {
            return;
        }

        if (actors is not null) {
            foreach (BlockActor actor in actors.Values)
                actor.OnWrite(actor.Nbt);
        }

        foreach (CompoundTag actor in blockActors.Values) {
            NbtCodec.Write(actor, ref writer, NetworkNbtOptions);
        }
    }

    /// <summary>Deserialized/Reads a Bedrock chunk body from a readers payload.</summary>
    /// <param name="x">The chunk X coordinate.</param>
    /// <param name="z">The chunk Z coordinate.</param>
    /// <param name="minimumSubChunkY">The first signed subchunk index in the payload.</param>
    /// <param name="subChunkCount">The subchunk count from the level chunk packet.</param>
    /// <param name="biomeCount">The number of biome storages required for the dimension height.</param>
    /// <param name="reader">The reader positioned at the start of the chunk body.</param>
    /// <returns>The decoded chunk.</returns>
    public static Chunk Deserialize(
        int x,
        int z,
        int minimumSubChunkY,
        int subChunkCount,
        int biomeCount,
        ref BinaryReader reader
    ) {
        if (subChunkCount is < 0 or > 256 || biomeCount is < 0 or > 256) {
            throw new ArgumentOutOfRangeException(nameof(subChunkCount), "Chunk section counts must be from 0 to 256.");
        }

        Chunk chunk = new(x, z);
        for (int i = 0; i < subChunkCount; i++) {
            int expectedIndex = checked(minimumSubChunkY + i);
            SubChunk? subChunk = SubChunk.Deserialize(ref reader, false);
            if (subChunk is null) {
                continue;
            }

            if (subChunk.Index != expectedIndex) {
                throw new InvalidDataException($"Subchunk index {subChunk.Index} does not match expected index {expectedIndex}.");
            }

            if (!subChunk.Empty) {
                (chunk.subChunks ??= []).Add(expectedIndex, subChunk);
            }
        }

        for (int i = 0; i < biomeCount; i++) {
            int subChunkY = checked(minimumSubChunkY + i);
            BiomeStorage? biomes = BiomeStorage.Deserialize(ref reader, false);
            if (biomes is { Empty: false }) {
                chunk.GetOrCreateSubChunk(subChunkY).Biomes = biomes;
            }
        }

        if (reader.Remaining > 0) {
            _ = reader.ReadUInt8();
        }

        while (reader.Remaining > 0) {
            if (reader.Buffer[reader.Offset] != 10) {
                throw new InvalidDataException("Invalid block actor NBT tag in chunk data.");
            }

            if (NbtCodec.Read(ref reader, NetworkNbtOptions) is not CompoundTag actor ||
                !actor.TryGet("x", out IntTag? actorX) ||
                !actor.TryGet("y", out IntTag? actorY) ||
                !actor.TryGet("z", out IntTag? actorZ)) {
                throw new InvalidDataException("Block actor is missing its coordinates.");
            }

            chunk.SetBlockNbt(actorX!.Value - x * 16, actorY!.Value, actorZ!.Value - z * 16, actor);
        }

        return chunk;
    }

    static void SetCoordinate(CompoundTag actor, string name, int value) {
        if (actor.TryGet(name, out IntTag? tag)) {
            tag!.Value = value;
        }
        else {
            actor.Add(name, new IntTag { Value = value });
        }
    }
}
