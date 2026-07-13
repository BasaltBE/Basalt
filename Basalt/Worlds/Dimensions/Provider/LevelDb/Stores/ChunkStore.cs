using System.Buffers;
using Basalt.Core.Profiling;
using Basalt.Protocol.Enums;
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
        return legacy is { Length: > 0 };
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
                return null;
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

        _entities.WriteChunkEntities(batch, chunk);
    }

    public void Delete(WriteBatch batch, DimensionType dimensionType, int x, int z)
    {
        _entities.DeleteChunkEntities(batch, dimensionType, x, z);
        batch.Delete(LevelDbKeyBuilder.BuildChunkKey(dimensionType, x, z));
        batch.Delete(LevelDbKeyBuilder.BuildBlockStorageListKey(dimensionType, x, z));
        batch.Delete(LevelDbKeyBuilder.BuildChunkKey(x, z));
        batch.Delete(LevelDbKeyBuilder.BuildBlockStorageListKey(x, z));
    }

    private byte[]? ReadBytes(DimensionType dimensionType, int x, int z)
    {
        byte[]? data = _database.Get(LevelDbKeyBuilder.BuildChunkKey(dimensionType, x, z));
        return data is { Length: > 0 } ? data : null;
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







