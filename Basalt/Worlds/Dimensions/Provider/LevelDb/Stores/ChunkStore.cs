using System.Buffers;
using Basalt.Core.Blocks;
using Basalt.Core.Profiling;
using Basalt.Core.Worlds.Dimensions.Chunk;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Io;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;

namespace Basalt.Core.Worlds.Dimensions.Provider;

internal sealed class ChunkStore {
    private readonly LevelDbDatabase _database;
    private readonly EntityStore _entities;

    public ChunkStore(LevelDbDatabase database, EntityStore entities) {
        _database = database;
        _entities = entities;
    }

    public bool Exists(DimensionType dimensionType, int x, int z) {
        byte[]? version = _database.Get(LevelDbKeyBuilder.BuildVersionKey(dimensionType, x, z));
        if (version is { Length: > 0 }) {
            return true;
        }

        byte[]? legacy = _database.Get(LevelDbKeyBuilder.BuildLegacyChunkKey(dimensionType, x, z));
        if (legacy is { Length: > 0 }) {
            return true;
        }

        legacy = _database.Get(LevelDbKeyBuilder.BuildLegacyChunkKey(x, z));
        return legacy is { Length: > 0 };
    }

    public ChunkColumn? Load(DimensionType dimensionType, int x, int z) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("ChunkStore.Load") : default;

        ChunkColumn? vanilla = LoadVanilla(dimensionType, x, z);
        if (vanilla is not null) {
            return vanilla;
        }

        byte[]? terrain = _database.Get(LevelDbKeyBuilder.BuildLegacyChunkKey(dimensionType, x, z));
        if (terrain is null || terrain.Length == 0) {
            terrain = _database.Get(LevelDbKeyBuilder.BuildLegacyChunkKey(x, z));
        }

        if (terrain is null || terrain.Length == 0) {
            return null;
        }

        ChunkColumn? chunk = DecodeChunk(terrain, dimensionType, x, z);
        if (chunk is null) {
            return null;
        }

        _entities.Load(chunk);

        chunk.Dirty = true;
        return chunk;
    }

    public void Save(LevelDbWriteBatch batch, ChunkColumn chunk) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("ChunkStore.Save") : default;

        batch.Put(LevelDbKeyBuilder.BuildVersionKey(chunk.Type, chunk.X, chunk.Z), [22]);

        int offset = chunk.Type == DimensionType.Overworld ? 4 : 0;
        int minIndex = chunk.Type == DimensionType.Overworld ? -4 : 0;
        int maxIndex = chunk.Type == DimensionType.Overworld ? 19 : 15;

        for (int i = minIndex; i <= maxIndex; i++) {
            int arrayIndex = chunk.Type == DimensionType.Overworld ? i + 4 : i;
            byte[] subChunkKey = LevelDbKeyBuilder.BuildSubChunkKey(chunk.Type, chunk.X, chunk.Z, (sbyte)i);

            if (arrayIndex < 0 || arrayIndex >= ChunkColumn.MaxSubChunks) {
                batch.Delete(subChunkKey);
                continue;
            }

            SubChunk? subChunk = chunk.SubChunks[arrayIndex];
            if (subChunk is null || subChunk.IsEmpty()) {
                batch.Delete(subChunkKey);
                continue;
            }

            batch.Put(subChunkKey, WriteSubChunkPayload(subChunk));
        }

        // Write Data3D (biomes).
        byte[] biomeData = WriteData3D(chunk);
        if (biomeData.Length > 0) {
            batch.Put(LevelDbKeyBuilder.BuildData3DKey(chunk.Type, chunk.X, chunk.Z), biomeData);
        }

        // Write block entities.
        byte[] blockEntityData = WriteBlockEntities(chunk);
        if (blockEntityData.Length > 0) {
            batch.Put(LevelDbKeyBuilder.BuildBlockEntityKey(chunk.Type, chunk.X, chunk.Z), blockEntityData);
        }
        else {
            batch.Delete(LevelDbKeyBuilder.BuildBlockEntityKey(chunk.Type, chunk.X, chunk.Z));
        }

        // Write entities.
        _entities.WriteChunkEntities(batch, chunk);

        // Delete legacy  keys.
        DeleteLegacyKeys(batch, chunk.Type, chunk.X, chunk.Z);
    }

    public void Delete(LevelDbWriteBatch batch, DimensionType dimensionType, int x, int z) {
        _entities.DeleteChunkEntities(batch, dimensionType, x, z);

        // Delete vanilla keys.
        batch.Delete(LevelDbKeyBuilder.BuildVersionKey(dimensionType, x, z));
        batch.Delete(LevelDbKeyBuilder.BuildData3DKey(dimensionType, x, z));
        batch.Delete(LevelDbKeyBuilder.BuildData2DKey(dimensionType, x, z));
        batch.Delete(LevelDbKeyBuilder.BuildBlockEntityKey(dimensionType, x, z));

        int minIndex = dimensionType == DimensionType.Overworld ? -4 : 0;
        int maxIndex = dimensionType == DimensionType.Overworld ? 19 : 15;
        for (int i = minIndex; i <= maxIndex; i++) {
            batch.Delete(LevelDbKeyBuilder.BuildSubChunkKey(dimensionType, x, z, (sbyte)i));
        }

        // Delete legacy  keys.
        DeleteLegacyKeys(batch, dimensionType, x, z);
    }

    private static void DeleteLegacyKeys(LevelDbWriteBatch batch, DimensionType dimensionType, int x, int z) {
        batch.Delete(LevelDbKeyBuilder.BuildLegacyChunkKey(dimensionType, x, z));
        batch.Delete(LevelDbKeyBuilder.BuildLegacyChunkKey(x, z));
        batch.Delete(LevelDbKeyBuilder.BuildLegacyBlockStorageListKey(dimensionType, x, z));
        batch.Delete(LevelDbKeyBuilder.BuildLegacyBlockStorageListKey(x, z));
    }

    private ChunkColumn? LoadVanilla(DimensionType dimensionType, int x, int z) {
        byte[]? version = _database.Get(LevelDbKeyBuilder.BuildVersionKey(dimensionType, x, z));
        if (version is not { Length: > 0 }) {
            return null;
        }

        SubChunk?[] subChunks = new SubChunk?[ChunkColumn.MaxSubChunks];

        int minIndex = dimensionType == DimensionType.Overworld ? -4 : 0;
        int maxIndex = dimensionType == DimensionType.Overworld ? 19 : 15;

        for (int i = minIndex; i <= maxIndex; i++) {
            byte[]? subChunkData = _database.Get(LevelDbKeyBuilder.BuildSubChunkKey(dimensionType, x, z, (sbyte)i));
            if (subChunkData is null || subChunkData.Length == 0) {
                continue;
            }

            try {
                int offset = 0;
                BinaryReader reader = new(subChunkData, ref offset);
                SubChunk subChunk = SubChunk.Deserialize(reader, nbt: true);
                subChunk.Index = (sbyte)i;

                int arrayIndex = dimensionType == DimensionType.Overworld ? i + 4 : i;
                if (arrayIndex >= 0 && arrayIndex < ChunkColumn.MaxSubChunks) {
                    subChunks[arrayIndex] = subChunk;
                }
            }
            catch (Exception ex) {
                Logger.Warn($"Failed loading subchunk {i} at {x},{z} in {dimensionType}: {ex.Message}");
            }
        }

        byte[]? biomeData = _database.Get(LevelDbKeyBuilder.BuildData3DKey(dimensionType, x, z));
        if (biomeData is { Length: > 0 }) {
            try {
                ParseData3D(biomeData, subChunks);
            }
            catch (Exception ex) {
                Logger.Warn($"Failed loading biomes at {x},{z} in {dimensionType}: {ex.Message}");
            }
        }

        ChunkColumn chunk = new(x, z, dimensionType, subChunks);

        byte[]? blockEntityData = _database.Get(LevelDbKeyBuilder.BuildBlockEntityKey(dimensionType, x, z));
        if (blockEntityData is { Length: > 0 }) {
            try {
                ParseBlockEntities(blockEntityData, chunk);
            }
            catch (Exception ex) {
                Logger.Warn($"Failed loading block entities at {x},{z} in {dimensionType}: {ex.Message}");
            }
        }

        _entities.Load(chunk);

        chunk.Dirty = true;
        return chunk;
    }

    private static void ParseData3D(byte[] data, SubChunk?[] subChunks) {
        int offset = 512;
        BinaryReader reader = new(data, ref offset);

        for (int i = 0; i < 25 && reader.Remaining > 0; i++) {
            BiomeStorage biomes = BiomeStorage.Deserialize(ref reader, disk: true);

            if (i >= subChunks.Length) {
                continue;
            }

            SubChunk? subChunk = subChunks[i];
            if (subChunk is not null) {
                subChunk.Biomes = biomes;
            }
        }
    }

    private static void ParseBlockEntities(byte[] data, ChunkColumn chunk) {
        int offset = 0;
        BinaryReader reader = new(data, ref offset);

        while (reader.Remaining > 0) {
            if ((TagType)reader.Buffer[reader.Offset] != TagType.Compound) {
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

    private static byte[] WriteSubChunkPayload(SubChunk subChunk) {
        int size = 64 * 1024;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

        while (true) {
            int offset = 0;
            BinaryWriter writer = new(buffer, ref offset);

            try {
                SubChunk.Serialize(subChunk, writer, nbt: true);
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

    private static byte[] WriteData3D(ChunkColumn chunk) {
        int size = 64 * 1024;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

        while (true) {
            int offset = 0;
            BinaryWriter writer = new(buffer, ref offset);

            try {
                // 512 bytes of heightmap placeholder (vanilla writes this).
                Span<byte> heightmap = stackalloc byte[512];
                heightmap.Clear();
                writer.WriteBytes(heightmap);

                for (int i = 0; i < ChunkColumn.MaxSubChunks; i++) {
                    SubChunk? subChunk = chunk.SubChunks[i];
                    if (subChunk is null || subChunk.IsEmpty()) {
                        continue;
                    }

                    BiomeStorage.Serialize(subChunk.Biomes, ref writer, disk: true);
                }

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

    private static byte[] WriteBlockEntities(ChunkColumn chunk) {
        List<BlockLevelStorage> blockEntities = chunk.GetAllBlockStorages();
        if (blockEntities.Count == 0) {
            return [];
        }

        int size = 16 * 1024;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

        while (true) {
            int offset = 0;
            BinaryWriter writer = new(buffer, ref offset);

            try {
                for (int i = 0; i < blockEntities.Count; i++) {
                    NBT.WriteTag(writer, blockEntities[i], new TagOptions(Name: true, Type: true, VarInt: false));
                }

                byte[] data = writer.GetProcessedBytes().ToArray();
                ArrayPool<byte>.Shared.Return(buffer);
                return data;
            }
            catch (Exception exception) when (
                exception is ArgumentOutOfRangeException or IndexOutOfRangeException) {
                ArrayPool<byte>.Shared.Return(buffer);
                size <<= 1;
                if (size > 64 * 1024 * 1024) {
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

    private static ChunkColumn? DecodeChunk(byte[] terrain, DimensionType dimensionType, int x, int z) {
        int offset = 0;
        BinaryReader reader = new(terrain, ref offset);
        try {
            return ChunkColumn.Deserialize(dimensionType, x, z, reader, nbt: true);
        }
        catch (Exception namedBiomeException) {
            offset = 0;
            reader = new(terrain, ref offset);
            try {
                return ChunkColumn.Deserialize(dimensionType, x, z, reader, nbt: true, biomeNbt: false);
            }
            catch {
                offset = 0;
                reader = new(terrain, ref offset);
                try {
                    return ChunkColumn.Deserialize(dimensionType, x, z, reader);
                }
                catch {
                    Logger.Warn($"Failed loading chunk {x},{z} in {dimensionType}: {namedBiomeException.Message}");
                    return null;
                }
            }
        }
    }

    private static byte[] WriteChunkPayload(ChunkColumn chunk) {
        int size = 2 * 1024 * 1024;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

        while (true) {
            int offset = 0;
            BinaryWriter writer = new(buffer, ref offset);

            try {
                ChunkColumn.Serialize(chunk, writer, nbt: true);
                byte[] data = writer.GetProcessedBytes().ToArray();
                ArrayPool<byte>.Shared.Return(buffer);
                return data;
            }
            catch (Exception exception) when (
                exception is ArgumentOutOfRangeException or IndexOutOfRangeException) {
                ArrayPool<byte>.Shared.Return(buffer);
                size <<= 1;
                if (size > 64 * 1024 * 1024) {
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







