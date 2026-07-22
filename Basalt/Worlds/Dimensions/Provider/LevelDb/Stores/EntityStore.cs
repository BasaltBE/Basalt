using System.Buffers;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Io;
using LevelDB;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;

namespace Basalt.Core.Worlds.Dimensions.Provider;

internal sealed class EntityStore {
    private const uint FormatVersion = 1;
    private static readonly TagOptions NbtOptions = new(Name: true, Type: true, VarInt: false);
    private readonly DB _database;

    public EntityStore(DB database) {
        _database = database;
    }

    public void Load(ChunkColumn chunk) {
        byte[]? entityList = _database.Get(LevelDbKeyBuilder.BuildEntityListKey(chunk.Type, chunk.X, chunk.Z));
        if (entityList is null || entityList.Length == 0) {
            entityList = _database.Get(LevelDbKeyBuilder.BuildEntityListKey(chunk.X, chunk.Z));
        }

        if (entityList is null || entityList.Length == 0) {
            return;
        }

        List<long>? uniqueIds = ReadEntityIds(entityList, chunk.Type, chunk.X, chunk.Z);
        if (uniqueIds is null) {
            return;
        }

        for (int i = 0; i < uniqueIds.Count; i++) {
            long uniqueId = uniqueIds[i];
            byte[]? entityData = _database.Get(LevelDbKeyBuilder.BuildEntityStorageKey(uniqueId));
            if (entityData is null || entityData.Length == 0) {
                continue;
            }

            CompoundTag? tag = ReadEntityPayload(entityData, chunk.Type, chunk.X, chunk.Z, uniqueId);
            if (tag is not null) {
                chunk.SetEntityStorage(uniqueId, tag, dirty: false);
            }
        }
    }

    public void WriteChunkEntities(WriteBatch batch, ChunkColumn chunk) {
        List<KeyValuePair<long, CompoundTag>> entities = chunk.GetAllEntityStorages();
        HashSet<long> oldIds = ReadSavedEntityIds(chunk.Type, chunk.X, chunk.Z);
        HashSet<long> newIds = new(entities.Select(entity => entity.Key));

        batch.Put(LevelDbKeyBuilder.BuildEntityListKey(chunk.Type, chunk.X, chunk.Z), WriteEntityList(entities));

        for (int i = 0; i < entities.Count; i++) {
            KeyValuePair<long, CompoundTag> entity = entities[i];
            batch.Put(LevelDbKeyBuilder.BuildEntityStorageKey(entity.Key), WriteEntityPayload(entity.Value));
        }

        foreach (long oldId in oldIds) {
            if (!newIds.Contains(oldId)) {
                batch.Delete(LevelDbKeyBuilder.BuildEntityStorageKey(oldId));
            }
        }
    }

    public void DeleteChunkEntities(WriteBatch batch, DimensionType dimensionType, int x, int z) {
        HashSet<long> uniqueIds = ReadSavedEntityIds(dimensionType, x, z);
        foreach (long uniqueId in uniqueIds) {
            batch.Delete(LevelDbKeyBuilder.BuildEntityStorageKey(uniqueId));
        }

        batch.Delete(LevelDbKeyBuilder.BuildEntityListKey(dimensionType, x, z));
        batch.Delete(LevelDbKeyBuilder.BuildEntityListKey(x, z));
    }

    private HashSet<long> ReadSavedEntityIds(DimensionType dimensionType, int x, int z) {
        HashSet<long> ids = [];
        AddEntityIds(ids, _database.Get(LevelDbKeyBuilder.BuildEntityListKey(dimensionType, x, z)), dimensionType, x, z);
        AddEntityIds(ids, _database.Get(LevelDbKeyBuilder.BuildEntityListKey(x, z)), dimensionType, x, z);
        return ids;
    }

    private static void AddEntityIds(HashSet<long> ids, byte[]? entityList, DimensionType dimensionType, int x, int z) {
        if (entityList is null || entityList.Length == 0) {
            return;
        }

        List<long>? read = ReadEntityIds(entityList, dimensionType, x, z);
        if (read is null) {
            return;
        }

        for (int i = 0; i < read.Count; i++) {
            ids.Add(read[i]);
        }
    }

    private static List<long>? ReadEntityIds(byte[] entityList, DimensionType dimensionType, int x, int z) {
        try {
            int offset = 0;
            BinaryReader reader = new(entityList, ref offset);
            return ReadEntityList(reader);
        }
        catch (Exception exception) {
            Logger.Warn($"Failed loading entity list for chunk {x},{z} in {dimensionType}: {exception.Message}");
            return null;
        }
    }

    private static CompoundTag? ReadEntityPayload(byte[] entityData, DimensionType dimensionType, int x, int z, long uniqueId) {
        try {
            int offset = 0;
            BinaryReader reader = new(entityData, ref offset);
            return ReadEntityPayload(reader);
        }
        catch (Exception exception) {
            Logger.Warn($"Failed loading entity {uniqueId} for chunk {x},{z} in {dimensionType}: {exception.Message}");
            return null;
        }
    }

    private static byte[] WriteEntityList(List<KeyValuePair<long, CompoundTag>> entities) {
        byte[] entityList = new byte[sizeof(uint) + sizeof(int) + entities.Count * sizeof(long)];
        int offset = 0;
        BinaryWriter writer = new(entityList, ref offset);
        writer.WriteUInt32(FormatVersion, littleEndian: true);
        writer.WriteInt32(entities.Count, littleEndian: true);

        for (int i = 0; i < entities.Count; i++) {
            writer.WriteInt64(entities[i].Key, littleEndian: true);
        }

        return entityList;
    }

    private static List<long> ReadEntityList(BinaryReader reader) {
        _ = reader.ReadUInt32(littleEndian: true);
        int count = reader.ReadInt32(littleEndian: true);
        if (count < 0 || count > reader.Remaining / sizeof(long)) {
            throw new InvalidDataException($"Invalid entity count {count}.");
        }

        List<long> ids = new(count);
        for (int i = 0; i < count; i++) {
            ids.Add(reader.ReadInt64(littleEndian: true));
        }

        return ids;
    }

    private static CompoundTag ReadEntityPayload(BinaryReader reader) {
        TagType type = (TagType)reader.ReadInt8();
        if (type != TagType.Compound) {
            throw new InvalidOperationException($"Expected Compound tag, got {type}.");
        }

        return CompoundTag.Read(reader, NbtOptions);
    }

    private static byte[] WriteEntityPayload(CompoundTag tag) {
        return WriteResizable(writer => NBT.WriteTag(writer, tag, NbtOptions));
    }

    private static byte[] WriteResizable(Action<BinaryWriter> write) {
        int size = 1024;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

        while (true) {
            int offset = 0;
            BinaryWriter writer = new(buffer, ref offset);

            try {
                write(writer);
                byte[] data = writer.GetProcessedBytes().ToArray();
                ArrayPool<byte>.Shared.Return(buffer);
                return data;
            }
            catch (Exception exception) when (
                exception is ArgumentOutOfRangeException or IndexOutOfRangeException) {
                ArrayPool<byte>.Shared.Return(buffer);
                size <<= 1;
                if (size > 16 * 1024 * 1024) {
                    throw;
                }

                buffer = ArrayPool<byte>.Shared.Rent(size);
            }
            catch {
                ArrayPool<byte>.Shared.Return(buffer);
                throw;
            }
        }
    }
}







