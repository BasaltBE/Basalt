using Basalt.Protocol.IO;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using LevelDB;
using System.Buffers;
using ChunkColumn = Basalt.World.Dimension.Chunk.Chunk;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.World.Dimension.Provider;

public sealed class LevelDbProvider : WorldProvider
{
    private const uint FormatVersion = 1;
    private static readonly ReadWriteOptions NbtOptions = new(Name: true, Type: true, VarInt: false);
    private readonly DB _database;
    public override string Identifier => "leveldb";

    public LevelDbProvider(string path)
    {
        Options options = new() { CreateIfMissing = true };
        _database = new DB(options, path);
    }

    public override bool HasChunk(DimensionType dimensionType, int x, int z)
    {
        return ReadChunkBytes(dimensionType, x, z) is not null;
    }

    public override ChunkColumn? LoadChunk(DimensionType dimensionType, int x, int z)
    {
        byte[]? terrain = ReadChunkBytes(dimensionType, x, z);
        if (terrain is null)
        {
            byte[] legacyChunkKey = new byte[9];
            LevelDbKeyBuilder.WriteChunkKey(legacyChunkKey, x, z);
            terrain = _database.Get(legacyChunkKey);
            if (terrain is null || terrain.Length == 0)
            {
                return null;
            }
        }

        ChunkColumn chunk;
        int offset = 0;
        BinaryReader reader = new(terrain, ref offset);
        try
        {
            chunk = ChunkColumn.Deserialize(dimensionType, x, z, reader, nbt: true);
        }
        catch (Exception namedBiomeException)
        {
            offset = 0;
            reader = new(terrain, ref offset);
            try
            {
                chunk = ChunkColumn.Deserialize(dimensionType, x, z, reader, nbt: true, biomeNbt: false);
            }
            catch
            {
                offset = 0;
                reader = new(terrain, ref offset);
                try
                {
                    chunk = ChunkColumn.Deserialize(dimensionType, x, z, reader);
                }
                catch
                {
                    Logger.Warn($"Failed loading chunk {x},{z} in {dimensionType}: {namedBiomeException.Message}");
                    return null;
                }
            }
        }

        LoadEntityStorages(chunk);
        chunk.Dirty = false;
        return chunk;
    }

    public override void SaveChunk(ChunkColumn chunk)
    {
        byte[] chunkKey = new byte[10];
        byte[] entityListKey = new byte[10];
        LevelDbKeyBuilder.WriteChunkKey(chunkKey, chunk.Type, chunk.X, chunk.Z);
        LevelDbKeyBuilder.WriteEntityListKey(entityListKey, chunk.Type, chunk.X, chunk.Z);
        PutChunk(chunkKey, chunk);

        List<KeyValuePair<long, Basalt.Protocol.Nbt.CompoundTag>> entities = chunk.GetAllEntityStorages();
        byte[] entityList = new byte[sizeof(uint) + sizeof(int) + entities.Count * sizeof(long)];
        int offset = 0;
        BinaryWriter writer = new(entityList, ref offset);
        WriteEntityList(writer, entities);
        _database.Put(entityListKey, entityList);

        byte[] entityStorageKey = new byte[9];
        for (int i = 0; i < entities.Count; i++)
        {
            KeyValuePair<long, CompoundTag> entity = entities[i];
            LevelDbKeyBuilder.WriteEntityStorageKey(entityStorageKey, entity.Key);
            PutEntityStorage(entityStorageKey, entity.Value);
        }
    }

    private void LoadEntityStorages(ChunkColumn chunk)
    {
        byte[]? entityList = _database.Get(LevelDbKeyBuilder.BuildEntityListKey(chunk.Type, chunk.X, chunk.Z));
        if (entityList is null || entityList.Length == 0)
        {
            entityList = _database.Get(LevelDbKeyBuilder.BuildEntityListKey(chunk.X, chunk.Z));
        }

        if (entityList is null || entityList.Length == 0)
        {
            return;
        }

        List<long> uniqueIds;
        try
        {
            int offset = 0;
            BinaryReader reader = new(entityList, ref offset);
            uniqueIds = ReadEntityList(reader);
        }
        catch (Exception exception)
        {
            Logger.Warn($"Failed loading entity list for chunk {chunk.X},{chunk.Z} in {chunk.Type}: {exception.Message}");
            return;
        }

        byte[] entityStorageKey = new byte[9];
        for (int i = 0; i < uniqueIds.Count; i++)
        {
            long uniqueId = uniqueIds[i];
            LevelDbKeyBuilder.WriteEntityStorageKey(entityStorageKey, uniqueId);
            byte[]? entityData = _database.Get(entityStorageKey);
            if (entityData is null || entityData.Length == 0)
            {
                continue;
            }

            try
            {
                int offset = 0;
                BinaryReader reader = new(entityData, ref offset);
                chunk.SetEntityStorage(uniqueId, ReadEntityStorage(reader), dirty: false);
            }
            catch (Exception exception)
            {
                Logger.Warn($"Failed loading entity {uniqueId} for chunk {chunk.X},{chunk.Z} in {chunk.Type}: {exception.Message}");
            }
        }
    }

    public override void DeleteChunk(DimensionType dimensionType, int x, int z)
    {
        byte[] chunkKey = new byte[10];
        byte[] blockListKey = new byte[10];
        byte[] entityListKey = new byte[10];
        LevelDbKeyBuilder.WriteChunkKey(chunkKey, dimensionType, x, z);
        LevelDbKeyBuilder.WriteBlockStorageListKey(blockListKey, dimensionType, x, z);
        LevelDbKeyBuilder.WriteEntityListKey(entityListKey, dimensionType, x, z);

        byte[]? entityList = _database.Get(entityListKey);
        if (entityList is not null && entityList.Length > 0)
        {
            byte[] entityStorageKey = new byte[9];
            int offset = 0;
            BinaryReader reader = new(entityList, ref offset);
            List<long> uniqueIds = ReadEntityList(reader);
            for (int i = 0; i < uniqueIds.Count; i++)
            {
                LevelDbKeyBuilder.WriteEntityStorageKey(entityStorageKey, uniqueIds[i]);
                _database.Delete(entityStorageKey);
            }
        }

        _database.Delete(chunkKey);
        _database.Delete(blockListKey);
        _database.Delete(entityListKey);

        byte[] legacyChunkKey = new byte[9];
        byte[] legacyBlockListKey = new byte[9];
        byte[] legacyEntityListKey = new byte[9];
        LevelDbKeyBuilder.WriteChunkKey(legacyChunkKey, x, z);
        LevelDbKeyBuilder.WriteBlockStorageListKey(legacyBlockListKey, x, z);
        LevelDbKeyBuilder.WriteEntityListKey(legacyEntityListKey, x, z);

        _database.Delete(legacyChunkKey);
        _database.Delete(legacyBlockListKey);
        _database.Delete(legacyEntityListKey);
    }

    public override void Dispose()
    {
        _database.Dispose();
    }

    public override CompoundTag? LoadPlayerData(string xuid)
    {
        if (string.IsNullOrWhiteSpace(xuid))
        {
            return null;
        }

        byte[]? data = _database.Get(LevelDbKeyBuilder.BuildPlayerStorageKey(xuid));
        if (data is null || data.Length == 0)
        {
            return null;
        }

        int offset = 0;
        BinaryReader reader = new(data, ref offset);
        return ReadEntityStorage(reader);
    }

    public override void SavePlayerData(string xuid, CompoundTag data)
    {
        if (string.IsNullOrWhiteSpace(xuid))
        {
            return;
        }

        PutEntityStorage(LevelDbKeyBuilder.BuildPlayerStorageKey(xuid), data);
    }

    private byte[]? ReadChunkBytes(DimensionType dimensionType, int x, int z)
    {
        byte[] key = new byte[10];
        LevelDbKeyBuilder.WriteChunkKey(key, dimensionType, x, z);
        byte[]? data = _database.Get(key);
        return data is { Length: > 0 } ? data : null;
    }

    private static CompoundTag ReadEntityStorage(BinaryReader reader)
    {
        TagType type = (TagType)reader.ReadInt8();
        if (type != TagType.Compound)
        {
            throw new InvalidOperationException($"Expected Compound tag, got {type}.");
        }

        return CompoundTag.Read(reader, NbtOptions, canHaveName: true);
    }

    private static void WriteEntityList(BinaryWriter writer, List<KeyValuePair<long, CompoundTag>> entities)
    {
        writer.WriteUInt32(FormatVersion, littleEndian: true);
        writer.WriteInt32(entities.Count, littleEndian: true);

        for (int i = 0; i < entities.Count; i++)
        {
            writer.WriteInt64(entities[i].Key, littleEndian: true);
        }
    }

    private static List<long> ReadEntityList(BinaryReader reader)
    {
        _ = reader.ReadUInt32(littleEndian: true);
        int count = reader.ReadInt32(littleEndian: true);
        if (count < 0 || count > reader.Remaining / sizeof(long))
        {
            throw new InvalidDataException($"Invalid entity count {count}.");
        }

        List<long> ids = new(count);
        for (int i = 0; i < count; i++)
        {
            ids.Add(reader.ReadInt64(littleEndian: true));
        }

        return ids;
    }

    private void PutChunk(byte[] key, ChunkColumn chunk)
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
                _database.Put(key, writer.GetProcessedBytes().ToArray());
                ArrayPool<byte>.Shared.Return(buffer);
                return;
            }
            catch (ArgumentOutOfRangeException)
            {
                ArrayPool<byte>.Shared.Return(buffer);
                size <<= 1;
                buffer = ArrayPool<byte>.Shared.Rent(size);
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(buffer);
                throw;
            }
        }
    }

    private void PutEntityStorage(byte[] key, CompoundTag tag)
    {
        int size = 1024;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

        while (true)
        {
            int offset = 0;
            BinaryWriter writer = new(buffer, ref offset);

            try
            {
                WriteEntityStorage(writer, tag);
                _database.Put(key, writer.GetProcessedBytes().ToArray());
                ArrayPool<byte>.Shared.Return(buffer);
                return;
            }
            catch (Exception exception) when (
                exception is ArgumentOutOfRangeException or IndexOutOfRangeException)
            {
                ArrayPool<byte>.Shared.Return(buffer);
                size <<= 1;
                if (size > 16 * 1024 * 1024)
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

    private static void WriteEntityStorage(BinaryWriter writer, CompoundTag tag)
    {
        NBT.WriteTag(writer, tag, NbtOptions, canHaveName: true);
    }
}
