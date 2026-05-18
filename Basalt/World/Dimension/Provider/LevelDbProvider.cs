using Basalt.Block;
using Basalt.Protocol.IO;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;
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
            return null;
        }

        ChunkColumn? chunk = ReadChunk(dimensionType, x, z, terrain);
        if (chunk is null)
        {
            DeleteChunk(dimensionType, x, z);
            return null;
        }

        LoadEntityStorages(chunk);
        chunk.Dirty = false;
        return chunk;
    }

    private byte[]? ReadChunkBytes(DimensionType dimensionType, int x, int z)
    {
        byte[]? terrain = _database.Get(LevelDbKeyBuilder.BuildChunkKey(dimensionType, x, z));
        if (terrain is not null && terrain.Length > 0)
        {
            return terrain;
        }

        terrain = _database.Get(LevelDbKeyBuilder.BuildChunkKey(x, z));
        return terrain is { Length: > 0 } ? terrain : null;
    }

    private static ChunkColumn? ReadChunk(DimensionType dimensionType, int x, int z, byte[] terrain)
    {
        try
        {
            return ChunkColumn.Deserialize(dimensionType, x, z, terrain, nbt: true);
        }
        catch (Exception namedBiomeException)
        {
            try
            {
                return ChunkColumn.Deserialize(dimensionType, x, z, terrain, nbt: true, biomeNbt: false);
            }
            catch
            {
                try
                {
                    return ChunkColumn.Deserialize(dimensionType, x, z, terrain);
                }
                catch
                {
                    Logger.Warn($"Failed loading chunk {x},{z} in {dimensionType}: {namedBiomeException.Message}");
                    return null;
                }
            }
        }
    }

    public override void SaveChunk(ChunkColumn chunk)
    {
        byte[] chunkKey = new byte[10];
        byte[] entityListKey = new byte[10];
        LevelDbKeyBuilder.WriteChunkKey(chunkKey, chunk.Type, chunk.X, chunk.Z);
        LevelDbKeyBuilder.WriteEntityListKey(entityListKey, chunk.Type, chunk.X, chunk.Z);
        _database.Put(chunkKey, ChunkColumn.Serialize(chunk, nbt: true));

        List<KeyValuePair<long, CompoundTag>> entities = chunk.GetAllEntityStorages();
        _database.Put(entityListKey, WriteEntityList(entities));

        byte[] entityStorageKey = new byte[9];
        for (int i = 0; i < entities.Count; i++)
        {
            KeyValuePair<long, CompoundTag> entity = entities[i];
            LevelDbKeyBuilder.WriteEntityStorageKey(entityStorageKey, entity.Key);
            _database.Put(entityStorageKey, WriteEntityStorage(entity.Value));
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
            uniqueIds = ReadEntityList(entityList);
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
                chunk.SetEntityStorage(uniqueId, ReadEntityStorage(entityData), dirty: false);
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
            try
            {
                List<long> uniqueIds = ReadEntityList(entityList);
                for (int i = 0; i < uniqueIds.Count; i++)
                {
                    LevelDbKeyBuilder.WriteEntityStorageKey(entityStorageKey, uniqueIds[i]);
                    _database.Delete(entityStorageKey);
                }
            }
            catch (Exception exception)
            {
                Logger.Warn($"Failed deleting entity storages for chunk {x},{z} in {dimensionType}: {exception.Message}");
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

        return ReadEntityStorage(data);
    }

    public override void SavePlayerData(string xuid, CompoundTag data)
    {
        if (string.IsNullOrWhiteSpace(xuid))
        {
            return;
        }

        _database.Put(LevelDbKeyBuilder.BuildPlayerStorageKey(xuid), WriteEntityStorage(data));
    }

    private static byte[] WriteEntityStorage(CompoundTag tag)
    {
        return WriteNbt(tag);
    }

    private static CompoundTag ReadEntityStorage(byte[] data)
    {
        BinaryReader reader = new(data);
        TagType type = (TagType)reader.ReadInt8();
        if (type != TagType.Compound)
        {
            throw new InvalidOperationException($"Expected Compound tag, got {type}.");
        }

        return CompoundTag.Read(ref reader, NbtOptions, canHaveName: true);
    }

    private static byte[] WriteEntityList(List<KeyValuePair<long, CompoundTag>> entities)
    {
        byte[] buffer = new byte[Math.Max(128, entities.Count * 8 + 16)];
        BinaryWriter writer = new(buffer);
        writer.WriteUInt32(FormatVersion, littleEndian: true);
        writer.WriteInt32(entities.Count, littleEndian: true);

        for (int i = 0; i < entities.Count; i++)
        {
            writer.WriteInt64(entities[i].Key, littleEndian: true);
        }

        return writer.GetBuffer().ToArray();
    }

    private static List<long> ReadEntityList(byte[] data)
    {
        BinaryReader reader = new(data);
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

    private static byte[] WriteNbt(BaseTag tag)
    {
        int size = 1024;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

        while (true)
        {
            BinaryWriter writer = new(buffer);

            try
            {
                NBT.WriteTag(ref writer, tag, NbtOptions, canHaveName: true);
                byte[] encoded = writer.GetBuffer().ToArray();
                ArrayPool<byte>.Shared.Return(buffer);
                return encoded;
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
        }
    }

}
