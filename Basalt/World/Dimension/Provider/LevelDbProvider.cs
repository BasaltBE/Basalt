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
        byte[] chunkKey = new byte[10];
        LevelDbKeyBuilder.WriteChunkKey(chunkKey, dimensionType, x, z);
        byte[]? terrain = _database.Get(chunkKey);
        if (terrain is null || terrain.Length == 0)
        {
            byte[] legacyChunkKey = new byte[9];
            LevelDbKeyBuilder.WriteChunkKey(legacyChunkKey, x, z);
            terrain = _database.Get(legacyChunkKey);
        }
        return terrain is not null && terrain.Length > 0;
    }

    public override ChunkColumn? LoadChunk(DimensionType dimensionType, int x, int z)
    {
        byte[] chunkKey = new byte[10];
        byte[] entityListKey = new byte[10];
        LevelDbKeyBuilder.WriteChunkKey(chunkKey, dimensionType, x, z);
        LevelDbKeyBuilder.WriteEntityListKey(entityListKey, dimensionType, x, z);

        byte[]? terrain = _database.Get(chunkKey);
        if (terrain is null || terrain.Length == 0)
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
        try
        {
            chunk = ChunkColumn.Deserialize(dimensionType, x, z, terrain, nbt: true);
        }
        catch
        {
            try
            {
                chunk = ChunkColumn.Deserialize(dimensionType, x, z, terrain, nbt: true, biomeNbt: false);
            }
            catch (Exception exception)
            {
                try
                {
                    chunk = ChunkColumn.Deserialize(dimensionType, x, z, terrain);
                }
                catch
                {
                    Logger.Warn($"Failed loading chunk {x},{z} in {dimensionType}: {exception.Message}");
                    DeleteChunk(dimensionType, x, z);
                    return null;
                }
            }
        }

        byte[]? entityList = _database.Get(entityListKey);
        if (entityList is null || entityList.Length == 0)
        {
            byte[] legacyEntityListKey = new byte[9];
            LevelDbKeyBuilder.WriteEntityListKey(legacyEntityListKey, x, z);
            entityList = _database.Get(legacyEntityListKey);
        }
        if (entityList is not null && entityList.Length > 0)
        {
            byte[] entityStorageKey = new byte[9];
            List<long> uniqueIds = DecodeEntityList(entityList);
            for (int i = 0; i < uniqueIds.Count; i++)
            {
                long uniqueId = uniqueIds[i];
                LevelDbKeyBuilder.WriteEntityStorageKey(entityStorageKey, uniqueId);
                byte[]? entityData = _database.Get(entityStorageKey);
                if (entityData is null || entityData.Length == 0)
                {
                    continue;
                }

                chunk.SetEntityStorage(uniqueId, DecodeEntityStorage(entityData), dirty: false);
            }
        }

        chunk.Dirty = false;
        return chunk;
    }

    public override void SaveChunk(ChunkColumn chunk)
    {
        byte[] chunkKey = new byte[10];
        byte[] entityListKey = new byte[10];
        LevelDbKeyBuilder.WriteChunkKey(chunkKey, chunk.Type, chunk.X, chunk.Z);
        LevelDbKeyBuilder.WriteEntityListKey(entityListKey, chunk.Type, chunk.X, chunk.Z);
        _database.Put(chunkKey, ChunkColumn.Serialize(chunk, nbt: true));

        List<KeyValuePair<long, Basalt.Protocol.Nbt.CompoundTag>> entities = chunk.GetAllEntityStorages();
        _database.Put(entityListKey, EncodeEntityList(entities));

        byte[] entityStorageKey = new byte[9];
        for (int i = 0; i < entities.Count; i++)
        {
            KeyValuePair<long, Basalt.Protocol.Nbt.CompoundTag> entity = entities[i];
            LevelDbKeyBuilder.WriteEntityStorageKey(entityStorageKey, entity.Key);
            _database.Put(entityStorageKey, EncodeEntityStorage(entity.Value));
        }
    }

    public override void DeleteChunk(DimensionType dimensionType, int x, int z)
    {
        byte[] chunkKey = new byte[10];
        byte[] blockListKey = new byte[10];
        byte[] entityListKey = new byte[10];
        //byte[] buffer = new byte[Math.Max(128, entities.Count * 8 + 16)];
        LevelDbKeyBuilder.WriteChunkKey(chunkKey, dimensionType, x, z);
        LevelDbKeyBuilder.WriteBlockStorageListKey(blockListKey, dimensionType, x, z);
        LevelDbKeyBuilder.WriteEntityListKey(entityListKey, dimensionType, x, z);

        byte[]? entityList = _database.Get(entityListKey);
        if (entityList is not null && entityList.Length > 0)
        {
            Span<byte> entityStorageKey = stackalloc byte[9];
            int offset = 0;
            List<long> uniqueIds = DecodeEntityList(new(entityList, ref offset));
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
        return DecodeEntityStorage(new(data, ref offset));
    }

    public override void SavePlayerData(string xuid, CompoundTag data)
    {
        if (string.IsNullOrWhiteSpace(xuid))
        {
            return;
        }

        _database.Put(LevelDbKeyBuilder.BuildPlayerStorageKey(xuid), EncodeEntityStorage(data));
    }

    private static byte[] EncodeEntityStorage(CompoundTag tag)
    {
        return EncodeNbt(tag);
    }

    private static CompoundTag DecodeEntityStorage(BinaryReader reader)
    {
        TagType type = (TagType)reader.ReadInt8();
        if (type != TagType.Compound)
        {
            throw new InvalidOperationException($"Expected Compound tag, got {type}.");
        }

        return CompoundTag.Read(ref reader, NbtOptions, canHaveName: true);
    }

    private static byte[] EncodeEntityList(BinaryWriter writer, List<KeyValuePair<long, CompoundTag>> entities)
    {
        writer.WriteUInt32(FormatVersion, littleEndian: true);
        writer.WriteInt32(entities.Count, littleEndian: true);

        for (int i = 0; i < entities.Count; i++)
        {
            writer.WriteInt64(entities[i].Key, littleEndian: true);
        }

        return writer.GetBuffer().ToArray();
    }

    private static List<long> DecodeEntityList(BinaryReader reader)
    {
        _ = reader.ReadUInt32(littleEndian: true);
        int count = reader.ReadInt32(littleEndian: true);

        List<long> ids = new(count);
        for (int i = 0; i < count; i++)
        {
            ids.Add(reader.ReadInt64(littleEndian: true));
        }

        return ids;
    }

    private static byte[] EncodeNbt(BaseTag tag)
    {
        int size = 1024;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

        while (true)
        {
            int offset = 0;
            BinaryWriter writer = new(buffer, ref offset);

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
