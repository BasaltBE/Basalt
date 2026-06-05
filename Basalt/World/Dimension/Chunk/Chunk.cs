using Basalt.Binary;
using Basalt.Core.Blocks;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Io;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;
using System.Buffers;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Core.World.Dimension.Chunk;

public sealed class Chunk
{
    public const int MaxSubChunks = 24;

    private readonly Dictionary<(int X, int Y, int Z), BlockLevelStorage> _blocks = [];
    private readonly Dictionary<(int X, int Y, int Z), Blocks.Block> _blockActors = [];
    private readonly Dictionary<long, CompoundTag> _entities = [];

    public DimensionType Type { get; }
    public int X { get; }
    public int Z { get; }
    public long Hash { get; }
    public SubChunk?[] SubChunks { get; }

    public byte[]? Cache;
    public bool Dirty;
    public bool Simulated;

    public Chunk(int x, int z, DimensionType type, SubChunk?[]? subChunks = null)
    {
        X = x;
        Z = z;
        Type = type;
        Hash = ((long)x << 32) | (uint)z;
        SubChunks = subChunks ?? new SubChunk?[MaxSubChunks];
    }

    public BlockPermutation GetPermutation(int x, int y, int z, int layer = 0)
    {
        SubChunk? subChunk = PeekSubChunk(y >> 4);
        if (subChunk is null)
        {
            return BlockPermutation.Resolve(BlockStorage.Air);
        }

        int state = subChunk.GetState(x & 0xF, y & 0xF, z & 0xF, layer);
        return BlockPermutation.Resolve(state);
    }

    public void SetPermutation(int x, int y, int z, BlockPermutation permutation, int layer = 0, bool dirty = true)
    {
        SubChunk subChunk = GetSubChunk(y >> 4);
        subChunk.SetState(x & 0xF, y & 0xF, z & 0xF, permutation.NetworkId, layer);

        if (dirty)
        {
            Dirty = true;
        }

        Cache = null;
    }

    public int GetBiome(int x, int y, int z)
    {
        SubChunk? subChunk = PeekSubChunk(y >> 4);
        return subChunk?.GetBiome(x & 0xF, y & 0xF, z & 0xF) ?? 0;
    }

    public void SetBiome(int x, int y, int z, int biomeId, bool dirty = true)
    {
        SubChunk subChunk = GetSubChunk(y >> 4);
        subChunk.SetBiome(x & 0xF, y & 0xF, z & 0xF, biomeId);

        if (dirty)
        {
            Dirty = true;
        }

        Cache = null;
    }

    public int GetTopmostLevel(int x, int z, int startY)
    {
        for (int y = startY; y >= -64; y--)
        {
            if (GetPermutation(x, y, z).Type.Identifier != "minecraft:air")
            {
                return y;
            }
        }

        return -64;
    }

    public int GetBottommostLevel(int x, int z, int endY)
    {
        for (int y = 0; y <= endY; y++)
        {
            if (GetPermutation(x, y, z).Type.Identifier != "minecraft:air")
            {
                return y;
            }
        }

        return 0;
    }

    public SubChunk GetSubChunk(int index)
    {
        int offset = Type == DimensionType.Overworld ? 4 : 0;

        if (index + offset < 0)
        {
            index = 0;
            offset = 0;
        }
        else if (index + offset >= MaxSubChunks)
        {
            index = MaxSubChunks - 1;
            offset = 0;
        }

        int resolved = index + offset;
        if (SubChunks[resolved] is null)
        {
            for (int i = 0; i <= resolved; i++)
            {
                if (SubChunks[i] is not null)
                {
                    continue;
                }

                SubChunk subChunk = new() { Index = (sbyte)(i - offset) };
                SubChunks[i] = subChunk;

                if (i == resolved)
                {
                    return subChunk;
                }
            }
        }

        return SubChunks[resolved]!;
    }

    public void SetSubChunk(int index, SubChunk subChunk)
    {
        int offset = Type == DimensionType.Overworld ? 4 : 0;

        if (index + offset < 0)
        {
            index = 0;
            offset = 0;
        }
        else if (index + offset >= MaxSubChunks)
        {
            index = MaxSubChunks - 1;
            offset = 0;
        }

        SubChunks[index + offset] = subChunk;
        Cache = null;
    }

    public int GetSubChunkSendCount()
    {
        int emptyTail = 0;
        for (int index = MaxSubChunks - 1; index >= 0; index--)
        {
            SubChunk? subChunk = SubChunks[index];
            if (subChunk is null || subChunk.IsEmpty())
            {
                emptyTail++;
                continue;
            }

            break;
        }

        return MaxSubChunks - emptyTail;
    }

    public List<BlockLevelStorage> GetAllBlockStorages()
    {
        return [.. _blocks.Values];
    }

    public bool HasBlockStorage(BlockPos position)
    {
        return _blocks.ContainsKey((position.X, position.Y, position.Z));
    }

    public BlockLevelStorage? GetBlockStorage(BlockPos position)
    {
        return _blocks.GetValueOrDefault((position.X, position.Y, position.Z));
    }

    public void SetBlockStorage(BlockPos position, BlockLevelStorage? data, bool dirty = true)
    {
        var key = (position.X, position.Y, position.Z);

        if (data is null)
        {
            _blocks.Remove(key);
        }
        else
        {
            data.SetPosition(position);
            _blocks[key] = data;
        }

        if (dirty)
        {
            Dirty = true;
            Cache = null; // Invalidate cache when block storage changes
        }
    }

    public bool HasBlockActor(BlockPos position)
    {
        return _blockActors.ContainsKey((position.X, position.Y, position.Z));
    }

    public Blocks.Block? GetBlockActor(BlockPos position)
    {
        return _blockActors.GetValueOrDefault((position.X, position.Y, position.Z));
    }

    public void SetBlockActor(BlockPos position, Blocks.Block? actor)
    {
        var key = (position.X, position.Y, position.Z);
        if (actor is null)
        {
            _blockActors.Remove(key);
            return;
        }

        _blockActors[key] = actor;
    }

    public List<KeyValuePair<(int X, int Y, int Z), Blocks.Block>> GetAllBlockActors()
    {
        return [.. _blockActors];
    }

    public List<KeyValuePair<long, CompoundTag>> GetAllEntityStorages()
    {
        return [.. _entities];
    }

    public bool HasEntityStorage(long uniqueId)
    {
        return _entities.ContainsKey(uniqueId);
    }

    public CompoundTag? GetEntityStorage(long uniqueId)
    {
        return _entities.GetValueOrDefault(uniqueId);
    }

    public void SetEntityStorage(long uniqueId, CompoundTag? data, bool dirty = true)
    {
        if (data is null)
        {
            _entities.Remove(uniqueId);
        }
        else
        {
            _entities[uniqueId] = data;
        }

        if (dirty)
        {
            Dirty = true;
        }
    }

    public bool IsEmpty()
    {
        for (int i = 0; i < SubChunks.Length; i++)
        {
            SubChunk? subChunk = SubChunks[i];
            if (subChunk is not null && !subChunk.IsEmpty())
            {
                return false;
            }
        }

        return true;
    }


    // I know this is bad
    public void ReleaseMemory()
    {
        Cache = null;
        _blocks.Clear();
        _blockActors.Clear();
        _entities.Clear();
        Array.Clear(SubChunks, 0, SubChunks.Length);
        Dirty = false;
    }

    public Chunk Insert(Chunk source)
    {
        if (X != source.X || Z != source.Z)
        {
            throw new InvalidOperationException("Cannot assign chunk with different coordinates.");
        }

        for (int i = 0; i < source.SubChunks.Length; i++)
        {
            if (source.SubChunks[i] is not null)
            {
                SubChunks[i] = source.SubChunks[i];
            }
        }

        Dirty = source.Dirty;
        Cache = source.Cache;

        foreach ((var key, BlockLevelStorage value) in source._blocks)
        {
            _blocks[key] = value;
        }

        foreach ((var key, Blocks.Block value) in source._blockActors)
        {
            _blockActors[key] = value;
        }

        foreach ((long key, CompoundTag value) in source._entities)
        {
            _entities[key] = value;
        }

        return this;
    }

    public static byte[] Serialize(Chunk chunk, bool nbt = false)
    {
        if (!nbt && chunk.Cache is not null)
        {
            return chunk.Cache;
        }

        using BinaryStream stream = BinaryStream.Rent(2 * 1024 * 1024);
        int written = Serialize(chunk, stream, nbt);
        byte[] serialized = stream.GetProcessedBytes().ToArray();
        if (nbt) return serialized;

        chunk.Cache = serialized;

        return chunk.Cache!;
    }

    public static int Serialize(Chunk chunk, BinaryWriter writer, bool nbt = false)
    {
        int subChunkCount = chunk.GetSubChunkSendCount();
        for (int index = 0; index < subChunkCount; index++)
        {
            int offset = chunk.Type == DimensionType.Overworld ? 4 : 0;
            SubChunk? subChunk = chunk.SubChunks[index];

            if (subChunk is null)
            {
                subChunk = new SubChunk { Index = (sbyte)(index - offset) };
            }

            SubChunk.Serialize(subChunk, writer, nbt);
        }

        for (int index = 0; index < subChunkCount; index++)
        {
            SubChunk? subChunk = chunk.SubChunks[index];
            if (subChunk is null || subChunk.IsEmpty())
            {
                continue;
            }

            BiomeStorage.Serialize(subChunk.Biomes, ref writer, nbt);
        }

        writer.WriteUInt8(0);

        foreach (KeyValuePair<(int X, int Y, int Z), Blocks.Block> actorEntry in chunk._blockActors)
        {
            (int x, int y, int z) = actorEntry.Key;
            BlockPos position = new() { X = x, Y = y, Z = z };

            BlockLevelStorage? storage = chunk.GetBlockStorage(position);
            if (storage is null)
            {
                storage = new BlockLevelStorage(chunk);
                storage.SetPosition(position);
                storage.Set("id", new StringTag { Value = Dimension.GetBlockActorId(actorEntry.Value.Type.Identifier) });
                storage.Set("isMovable", new ByteTag { Value = 1 });
                chunk.SetBlockStorage(position, storage, dirty: false);
            }
            else if (storage.Get<StringTag>("id") is not { } idTag || string.IsNullOrWhiteSpace(idTag.Value))
            {
                storage.Set("id", new StringTag { Value = Dimension.GetBlockActorId(actorEntry.Value.Type.Identifier) });
            }

            actorEntry.Value.WriteTraits(storage);
        }

        List<BlockLevelStorage> blockEntities = chunk.GetAllBlockStorages();
        for (int i = 0; i < blockEntities.Count; i++)
        {
            NBT.WriteTag(writer, blockEntities[i], new TagOptions(Name: true, Type: true, VarInt: false));
        }

        return writer.Offset;
    }

    public static Chunk Deserialize(DimensionType type, int x, int z, BinaryReader reader, bool nbt = false, bool? biomeNbt = null)
    {
        SubChunk?[] subChunks = new SubChunk?[MaxSubChunks];

        for (int index = 0; index < MaxSubChunks; index++)
        {
            if (reader.Remaining <= 0)
            {
                break;
            }

            byte header = reader.Buffer[reader.Offset];
            if (header != 8 && header != 9)
            {
                break;
            }

            subChunks[index] = SubChunk.Deserialize(reader, nbt);
        }

        for (int i = 0; i < subChunks.Length; i++)
        {
            SubChunk? subChunk = subChunks[i];
            if (subChunk is null || subChunk.IsEmpty())
            {
                continue;
            }

            subChunk.Biomes = BiomeStorage.Deserialize(ref reader, biomeNbt ?? nbt);
        }

        if (reader.Remaining > 0)
        {
            _ = reader.ReadUInt8();
        }

        Chunk chunk = new(x, z, type, subChunks);

        while (reader.Remaining > 0)
        {
            if ((TagType)reader.Buffer[reader.Offset] != TagType.Compound)
            {
                break;
            }

            CompoundTag tag = NBT.ReadTag<CompoundTag>(
                reader,
                new TagOptions(Name: true, Type: true, VarInt: false)
                );

            BlockLevelStorage storage = new(chunk, tag);
            chunk.SetBlockStorage(storage.GetPosition(), storage, dirty: false);
        }


        chunk.Cache = nbt ? null : reader.GetProcessedBytes().ToArray();

        return chunk;
    }

    private SubChunk? PeekSubChunk(int index)
    {
        int offset = Type == DimensionType.Overworld ? 4 : 0;
        int resolved = index + offset;

        if (resolved < 0 || resolved >= MaxSubChunks)
        {
            return null;
        }

        return SubChunks[resolved];
    }


}







