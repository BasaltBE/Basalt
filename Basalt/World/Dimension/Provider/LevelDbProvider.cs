using Basalt.Block;
using Basalt.Protocol.IO;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;
using LevelDB;
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

    public override bool HasChunk(int x, int z)
    {
        byte[] chunkKey = new byte[9];
        LevelDbKeyBuilder.WriteChunkKey(chunkKey, x, z);
        byte[]? terrain = _database.Get(chunkKey);
        return terrain is not null && terrain.Length > 0;
    }

    public override ChunkColumn? LoadChunk(DimensionType dimensionType, int x, int z)
    {
        byte[] chunkKey = new byte[9];
        byte[] blockListKey = new byte[9];
        byte[] entityListKey = new byte[9];
        LevelDbKeyBuilder.WriteChunkKey(chunkKey, x, z);
        LevelDbKeyBuilder.WriteBlockStorageListKey(blockListKey, x, z);
        LevelDbKeyBuilder.WriteEntityListKey(entityListKey, x, z);

        byte[]? terrain = _database.Get(chunkKey);
        if (terrain is null || terrain.Length == 0)
        {
            return null;
        }

        ChunkColumn chunk = ChunkColumn.Deserialize(dimensionType, x, z, terrain);

        byte[]? blockList = _database.Get(blockListKey);
        if (blockList is not null && blockList.Length > 0)
        {
            byte[] blockStorageKey = new byte[13];
            List<BlockPos> positions = DecodeBlockStorageList(blockList);
            for (int i = 0; i < positions.Count; i++)
            {
                BlockPos pos = positions[i];
                LevelDbKeyBuilder.WriteBlockStorageKey(blockStorageKey, pos);
                byte[]? blockData = _database.Get(blockStorageKey);
                if (blockData is null || blockData.Length == 0)
                {
                    continue;
                }

                BlockLevelStorage storage = DecodeBlockStorage(chunk, blockData);
                chunk.SetBlockStorage(pos, storage, dirty: false);
            }
        }

        byte[]? entityList = _database.Get(entityListKey);
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
        byte[] chunkKey = new byte[9];
        byte[] blockListKey = new byte[9];
        byte[] entityListKey = new byte[9];
        LevelDbKeyBuilder.WriteChunkKey(chunkKey, chunk.X, chunk.Z);
        LevelDbKeyBuilder.WriteBlockStorageListKey(blockListKey, chunk.X, chunk.Z);
        LevelDbKeyBuilder.WriteEntityListKey(entityListKey, chunk.X, chunk.Z);
        _database.Put(chunkKey, ChunkColumn.Serialize(chunk));

        List<BlockLevelStorage> blockStorages = chunk.GetAllBlockStorages();
        _database.Put(blockListKey, EncodeBlockStorageList(blockStorages));

        byte[] blockStorageKey = new byte[13];
        for (int i = 0; i < blockStorages.Count; i++)
        {
            BlockLevelStorage storage = blockStorages[i];
            BlockPos pos = storage.GetPosition();
            LevelDbKeyBuilder.WriteBlockStorageKey(blockStorageKey, pos);
            _database.Put(blockStorageKey, EncodeBlockStorage(storage));
        }

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

    public override void DeleteChunk(int x, int z)
    {
        byte[] chunkKey = new byte[9];
        byte[] blockListKey = new byte[9];
        byte[] entityListKey = new byte[9];
        LevelDbKeyBuilder.WriteChunkKey(chunkKey, x, z);
        LevelDbKeyBuilder.WriteBlockStorageListKey(blockListKey, x, z);
        LevelDbKeyBuilder.WriteEntityListKey(entityListKey, x, z);

        byte[]? blockList = _database.Get(blockListKey);
        if (blockList is not null && blockList.Length > 0)
        {
            byte[] blockStorageKey = new byte[13];
            List<BlockPos> positions = DecodeBlockStorageList(blockList);
            for (int i = 0; i < positions.Count; i++)
            {
                LevelDbKeyBuilder.WriteBlockStorageKey(blockStorageKey, positions[i]);
                _database.Delete(blockStorageKey);
            }
        }

        byte[]? entityList = _database.Get(entityListKey);
        if (entityList is not null && entityList.Length > 0)
        {
            byte[] entityStorageKey = new byte[9];
            List<long> uniqueIds = DecodeEntityList(entityList);
            for (int i = 0; i < uniqueIds.Count; i++)
            {
                LevelDbKeyBuilder.WriteEntityStorageKey(entityStorageKey, uniqueIds[i]);
                _database.Delete(entityStorageKey);
            }
        }

        _database.Delete(chunkKey);
        _database.Delete(blockListKey);
        _database.Delete(entityListKey);
    }

    public override void Dispose()
    {
        _database.Dispose();
    }

    private static byte[] EncodeBlockStorage(BlockLevelStorage storage)
    {
        return EncodeNbt(storage);
    }

    private static BlockLevelStorage DecodeBlockStorage(ChunkColumn chunk, byte[] data)
    {
        return BlockLevelStorage.FromBuffer(chunk, data);
    }

    private static byte[] EncodeEntityStorage(CompoundTag tag)
    {
        return EncodeNbt(tag);
    }

    private static CompoundTag DecodeEntityStorage(byte[] data)
    {
        BinaryReader reader = new(data);
        TagType type = (TagType)reader.ReadInt8();
        if (type != TagType.Compound)
        {
            throw new InvalidOperationException($"Expected Compound tag, got {type}.");
        }

        return CompoundTag.Read(ref reader, NbtOptions, canHaveName: true);
    }

    private static byte[] EncodeBlockStorageList(List<BlockLevelStorage> storages)
    {
        byte[] buffer = new byte[Math.Max(128, storages.Count * 16 + 16)];
        BinaryWriter writer = new(buffer);
        writer.WriteUInt32(FormatVersion, littleEndian: true);
        writer.WriteInt32(storages.Count, littleEndian: true);

        for (int i = 0; i < storages.Count; i++)
        {
            BlockPos pos = storages[i].GetPosition();
            writer.WriteInt32(pos.X, littleEndian: true);
            writer.WriteInt32(pos.Y, littleEndian: true);
            writer.WriteInt32(pos.Z, littleEndian: true);
        }

        return writer.GetBuffer().ToArray();
    }

    private static List<BlockPos> DecodeBlockStorageList(byte[] data)
    {
        BinaryReader reader = new(data);
        _ = reader.ReadUInt32(littleEndian: true);
        int count = reader.ReadInt32(littleEndian: true);

        List<BlockPos> positions = new(count);
        for (int i = 0; i < count; i++)
        {
            positions.Add(new BlockPos
            {
                X = reader.ReadInt32(littleEndian: true),
                Y = reader.ReadInt32(littleEndian: true),
                Z = reader.ReadInt32(littleEndian: true)
            });
        }

        return positions;
    }

    private static byte[] EncodeEntityList(List<KeyValuePair<long, CompoundTag>> entities)
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

    private static List<long> DecodeEntityList(byte[] data)
    {
        BinaryReader reader = new(data);
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

        while (true)
        {
            byte[] buffer = new byte[size];
            BinaryWriter writer = new(buffer);

            try
            {
                NBT.WriteTag(ref writer, tag, NbtOptions, canHaveName: true);
                return writer.GetBuffer().ToArray();
            }
            catch (ArgumentOutOfRangeException)
            {
                size <<= 1;
                if (size > 16 * 1024 * 1024)
                {
                    throw;
                }
            }
        }
    }
}
