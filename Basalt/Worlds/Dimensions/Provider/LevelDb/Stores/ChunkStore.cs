using System.Buffers;
using Basalt.Core.Blocks;
using Basalt.Core.Profiling;
using Basalt.Core.Worlds.Dimensions.Chunk;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Io;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;
using LevelDB;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;

namespace Basalt.Core.Worlds.Dimensions.Provider;

internal sealed class ChunkStore
{
    private readonly DB _database;
    private readonly EntityStore _entities;

    public ChunkStore(DB database, EntityStore entities)
    {
        _database = database;
        _entities = entities;
    }

    public bool Exists(DimensionType dimensionType, int x, int z)
    {
        if (ReadBytes(dimensionType, x, z) is not null)
        {
            return true;
        }

        byte[]? legacy = _database.Get(LevelDbKeyBuilder.BuildChunkKey(x, z));
        if (legacy is { Length: > 0 })
        {
            return true;
        }

        byte[]? version = _database.Get(LevelDbKeyBuilder.BuildVersionKey(dimensionType, x, z));
        return version is { Length: > 0 };
    }

    public ChunkColumn? Load(DimensionType dimensionType, int x, int z)
    {
        using var __zone = Profiler.BeginZone("ChunkStore.Load");
        byte[]? terrain = ReadBytes(dimensionType, x, z);
        bool fromLegacy = false;
        if (terrain is null)
        {
            terrain = _database.Get(LevelDbKeyBuilder.BuildChunkKey(x, z));
            if (terrain is null || terrain.Length == 0)
            {
                return LoadVanilla(dimensionType, x, z);
            }

            fromLegacy = true;
        }

        ChunkColumn? chunk = DecodeChunk(terrain, dimensionType, x, z);
        if (chunk is null)
        {
            return null;
        }

        _entities.Load(chunk);

        chunk.Dirty = fromLegacy;
        return chunk;
    }

    public void Save(WriteBatch batch, ChunkColumn chunk)
    {
        using var __zone = Profiler.BeginZone("ChunkStore.Save");
        byte[] terrain = WriteChunkPayload(chunk);

        batch.Put(LevelDbKeyBuilder.BuildChunkKey(chunk.Type, chunk.X, chunk.Z), terrain);

        batch.Delete(LevelDbKeyBuilder.BuildChunkKey(chunk.X, chunk.Z));
        DeleteVanillaKeys(batch, chunk.Type, chunk.X, chunk.Z);

        _entities.WriteChunkEntities(batch, chunk);
    }

    public void Delete(WriteBatch batch, DimensionType dimensionType, int x, int z)
    {
        _entities.DeleteChunkEntities(batch, dimensionType, x, z);
        batch.Delete(LevelDbKeyBuilder.BuildChunkKey(dimensionType, x, z));
        batch.Delete(LevelDbKeyBuilder.BuildBlockStorageListKey(dimensionType, x, z));
        batch.Delete(LevelDbKeyBuilder.BuildChunkKey(x, z));
        batch.Delete(LevelDbKeyBuilder.BuildBlockStorageListKey(x, z));
        DeleteVanillaKeys(batch, dimensionType, x, z);
    }

    private byte[]? ReadBytes(DimensionType dimensionType, int x, int z)
    {
        byte[]? data = _database.Get(LevelDbKeyBuilder.BuildChunkKey(dimensionType, x, z));
        return data is { Length: > 0 } ? data : null;
    }

    private ChunkColumn? LoadVanilla(DimensionType dimensionType, int x, int z)
    {
        byte[]? version = _database.Get(LevelDbKeyBuilder.BuildVersionKey(dimensionType, x, z));
        if (version is not { Length: > 0 })
        {
            return null;
        }

        SubChunk?[] subChunks = new SubChunk?[ChunkColumn.MaxSubChunks];

        int minIndex = dimensionType == DimensionType.Overworld ? -4 : 0;
        int maxIndex = dimensionType == DimensionType.Overworld ? 19 : 15;

        for (int i = minIndex; i <= maxIndex; i++)
        {
            byte[]? subChunkData = _database.Get(LevelDbKeyBuilder.BuildSubChunkKey(dimensionType, x, z, (sbyte)i));
            if (subChunkData is null || subChunkData.Length == 0)
            {
                continue;
            }

            try
            {
                int offset = 0;
                BinaryReader reader = new(subChunkData, ref offset);
                SubChunk subChunk = SubChunk.Deserialize(reader, nbt: true);
                subChunk.Index = (sbyte)i;

                int arrayIndex = dimensionType == DimensionType.Overworld ? i + 4 : i;
                if (arrayIndex >= 0 && arrayIndex < ChunkColumn.MaxSubChunks)
                {
                    subChunks[arrayIndex] = subChunk;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed loading subchunk {i} at {x},{z} in {dimensionType}: {ex.Message}");
            }
        }

        byte[]? biomeData = _database.Get(LevelDbKeyBuilder.BuildData3DKey(dimensionType, x, z));
        if (biomeData is { Length: > 0 })
        {
            try
            {
                ParseData3D(biomeData, subChunks);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed loading biomes at {x},{z} in {dimensionType}: {ex.Message}");
            }
        }

        ChunkColumn chunk = new(x, z, dimensionType, subChunks);

        byte[]? blockEntityData = _database.Get(LevelDbKeyBuilder.BuildBlockEntityKey(dimensionType, x, z));
        if (blockEntityData is { Length: > 0 })
        {
            try
            {
                ParseBlockEntities(blockEntityData, chunk);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed loading block entities at {x},{z} in {dimensionType}: {ex.Message}");
            }
        }

        _entities.Load(chunk);

        chunk.Dirty = true;
        return chunk;
    }

    private static void ParseData3D(byte[] data, SubChunk?[] subChunks)
    {
        int offset = 512;
        BinaryReader reader = new(data, ref offset);

        for (int i = 0; i < 25 && reader.Remaining > 0; i++)
        {
            BiomeStorage biomes = BiomeStorage.Deserialize(ref reader, disk: true);

            if (i >= subChunks.Length)
            {
                continue;
            }

            SubChunk? subChunk = subChunks[i];
            if (subChunk is not null)
            {
                subChunk.Biomes = biomes;
            }
        }
    }

    private static void ParseBlockEntities(byte[] data, ChunkColumn chunk)
    {
        int offset = 0;
        BinaryReader reader = new(data, ref offset);

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
    }

    private static void DeleteVanillaKeys(WriteBatch batch, DimensionType dimensionType, int x, int z)
    {
        batch.Delete(LevelDbKeyBuilder.BuildVersionKey(dimensionType, x, z));
        batch.Delete(LevelDbKeyBuilder.BuildData3DKey(dimensionType, x, z));
        batch.Delete(LevelDbKeyBuilder.BuildData2DKey(dimensionType, x, z));
        batch.Delete(LevelDbKeyBuilder.BuildBlockEntityKey(dimensionType, x, z));

        int minIndex = dimensionType == DimensionType.Overworld ? -4 : 0;
        int maxIndex = dimensionType == DimensionType.Overworld ? 19 : 15;
        for (int i = minIndex; i <= maxIndex; i++)
        {
            batch.Delete(LevelDbKeyBuilder.BuildSubChunkKey(dimensionType, x, z, (sbyte)i));
        }
    }

    private static ChunkColumn? DecodeChunk(byte[] terrain, DimensionType dimensionType, int x, int z)
    {
        int offset = 0;
        BinaryReader reader = new(terrain, ref offset);
        try
        {
            return ChunkColumn.Deserialize(dimensionType, x, z, reader, nbt: true);
        }
        catch (Exception namedBiomeException)
        {
            offset = 0;
            reader = new(terrain, ref offset);
            try
            {
                return ChunkColumn.Deserialize(dimensionType, x, z, reader, nbt: true, biomeNbt: false);
            }
            catch
            {
                offset = 0;
                reader = new(terrain, ref offset);
                try
                {
                    return ChunkColumn.Deserialize(dimensionType, x, z, reader);
                }
                catch
                {
                    Logger.Warn($"Failed loading chunk {x},{z} in {dimensionType}: {namedBiomeException.Message}");
                    return null;
                }
            }
        }
    }

    private static byte[] WriteChunkPayload(ChunkColumn chunk)
    {
        int size = 2 * 1024 * 1024;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

        while (true)
        {
            int offset = 0;
            BinaryWriter writer = new(buffer, ref offset);

            try
            {
                ChunkColumn.Serialize(chunk, writer, nbt: true);
                byte[] data = writer.GetProcessedBytes().ToArray();
                ArrayPool<byte>.Shared.Return(buffer);
                return data;
            }
            catch (Exception exception) when (
                exception is ArgumentOutOfRangeException or IndexOutOfRangeException)
            {
                ArrayPool<byte>.Shared.Return(buffer);
                size <<= 1;
                if (size > 64 * 1024 * 1024)
                {
                    throw;
                }

                buffer = ArrayPool<byte>.Shared.Rent(size);
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(buffer);
                throw;
            }
        }
    }
}







