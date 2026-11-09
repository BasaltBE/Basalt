using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Providers;
using BasaltBE.Basalt.Worlds.Providers.Types;
using BasaltBE.Nbt.Types;
using Basalt.Binary;
using NbtCodec = BasaltBE.Nbt.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.Worlds.Dimensions.Chunks;

public sealed class Chunk(int x, int z) {
    static readonly Options NetworkNbtOptions = new() { VarInt = true, StringLengthVarInt = true };
    static readonly NbtReadLimits PendingTickReadLimits = new(32, 4_194_304, 262_144, 1_048_576);
    Dictionary<int, SubChunk>? subChunks;
    byte[]? upperBiomeData;
    internal Dictionary<(int X, int Y, int Z), CompoundTag>? blockActors;
    Dictionary<(int X, int Y, int Z), BlockActor>? actors;
    internal Dimension? Dimension;
    internal long Revision;
    internal CompoundTag? PendingTicks;
    internal bool PendingTicksInvalid;
    internal bool PendingTicksRecovered;

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
            if (blockActors is { Count: > 0 } || PendingTicks is not null) {
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

        subChunk = new SubChunk {
            Index = checked((sbyte)subChunkY),
            Biomes = new BiomeStorage(Dimension?.CustomDefinition?.DefaultBiomeId ?? 0)
        };
        subChunk.Chunk = this;
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
            bool changed = blockActors?.Remove((x, y, z)) == true;
            actors?.Remove((x, y, z));
            if (changed) MarkDirty();
            return;
        }

        actor.Name = string.Empty;
        SetCoordinate(actor, "x", checked(X * 16 + x));
        SetCoordinate(actor, "y", y);
        SetCoordinate(actor, "z", checked(Z * 16 + z));
        blockActors ??= [];
        blockActors[(x, y, z)] = actor;
        MarkDirty();
    }

    internal BlockActor? GetRuntimeActor(int x, int y, int z) => actors?.GetValueOrDefault((x, y, z));

    internal void SetRuntimeActor(int x, int y, int z, BlockActor? actor) {
        if (actor is null) {
            actors?.Remove((x, y, z));
            return;
        }

        (actors ??= [])[(x, y, z)] = actor;
    }

    internal void MarkDirty() {
        Revision++;
        Dimension?.MarkChunkDirty(this);
    }

    /// <summary>Serializes/Writes chunk data into a writer.</summary>
    /// <param name="minimumSubChunkY">The first signed subchunk index in the payload.</param>
    /// <param name="subChunkCount">The number of subchunks requested by the level chunk packet.</param>
    /// <param name="biomeCount">The number of biome storages required for the dimension height.</param>
    public void Serialize(ref BinaryWriter writer, int minimumSubChunkY, int subChunkCount, int biomeCount, int defaultBiome = 0) {
        if (subChunkCount is < 0 or > 256 || biomeCount is < 0 or > 256) {
            throw new ArgumentOutOfRangeException(nameof(subChunkCount), "Chunk section counts must be from 0 to 256.");
        }

        for (int i = 0; i < subChunkCount; i++) {
            int subChunkY = checked(minimumSubChunkY + i);
            sbyte index = checked((sbyte)subChunkY);
            if (GetSubChunk(subChunkY) is { } subChunk) {
                subChunk.Serialize(ref writer, index);
            } else {
                writer.WriteUInt8(9);
                writer.WriteUInt8(0);
                writer.WriteInt8(index);
            }
        }

        for (int i = 0; i < biomeCount; i++) {
            int subChunkY = checked(minimumSubChunkY + i);
            if (GetSubChunk(subChunkY) is { Biomes.Initialized: true } subChunk) {
                subChunk.Biomes.Serialize(ref writer);
            } else {
                BiomeStorage.SerializeDefault(ref writer, defaultBiome);
            }
        }

        writer.WriteUInt8(0);
        if (blockActors is null) {
            return;
        }

        if (actors is not null) {
            foreach (BlockActor actor in actors.Values) actor.OnWrite(actor.Nbt);
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
            SubChunk? subChunk = SubChunk.Deserialize(ref reader, ChunkSerializationMode.Network, false);
            if (subChunk is null) {
                continue;
            }

            if (subChunk.Index != expectedIndex) {
                throw new InvalidDataException($"Subchunk index {subChunk.Index} does not match expected index {expectedIndex}.");
            }

            if (!subChunk.Empty) {
                subChunk.Chunk = chunk;
                (chunk.subChunks ??= []).Add(expectedIndex, subChunk);
            }
        }

        for (int i = 0; i < biomeCount; i++) {
            int subChunkY = checked(minimumSubChunkY + i);
            BiomeStorage? biomes = BiomeStorage.Deserialize(ref reader, ChunkSerializationMode.Network, false);
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

    public ChunkStorageEntry[] Serialize() {
        int minimumY = Dimension?.MinimumY ?? -64;
        int maximumY = Dimension?.MaximumY ?? 319;
        int minimumSubChunkY = minimumY >> 4;
        int subChunkCount = (Dimension?.HeightRange ?? 384) >> 4;
        List<ChunkStorageEntry> entries = new(subChunkCount + 2);
        using (BinaryStream stream = BinaryStream.Rent(64 * 1024)) {
            BinaryWriter writer = stream.GetWriter();
            for (int z = 0; z < 16; z++) {
                for (int x = 0; x < 16; x++) {
                    int height = 0;
                    for (int y = maximumY; y >= minimumY; y--) {
                        BlockPermutation permutation = GetPermutation(x, y, z);
                        BlockType? type = BlockPalette.FindType(permutation.Identifier);
                        if (type is { Opacity: > 0 } or { Solid: true } or { Liquid: true }) {
                            height = y - minimumY + 1;
                            break;
                        }
                    }
                    writer.WriteUInt16(checked((ushort)height), true);
                }
            }

            for (int subChunkY = minimumSubChunkY; subChunkY < minimumSubChunkY + subChunkCount; subChunkY++) {
                SubChunk? subChunk = GetSubChunk(subChunkY);
                if (subChunk is null) {
                    writer.WriteUInt8(0xFF);
                } else if (!subChunk.Biomes.Initialized) {
                    BiomeStorage.SerializeDefault(
                        ref writer,
                        Dimension?.CustomDefinition?.DefaultBiomeId ?? 0,
                        ChunkSerializationMode.LevelDb
                    );
                } else {
                    subChunk.Biomes.Serialize(ref writer, ChunkSerializationMode.LevelDb);
                }
            }
            if (upperBiomeData is not null) writer.WriteBytes(upperBiomeData);
            entries.Add(new ChunkStorageEntry(ChunkStorageType.Data3D, 0, stream.Buffer[..stream.Offset].ToArray()));
        }

        for (int subChunkY = minimumSubChunkY; subChunkY < minimumSubChunkY + subChunkCount; subChunkY++) {
            if (GetSubChunk(subChunkY) is not { Empty: false } subChunk) continue;

            using BinaryStream stream = BinaryStream.Rent(64 * 1024);
            BinaryWriter writer = stream.GetWriter();
            subChunk.Serialize(ref writer, checked((sbyte)subChunkY), ChunkSerializationMode.LevelDb);
            entries.Add(new ChunkStorageEntry(ChunkStorageType.SubChunk, checked((sbyte)subChunkY), stream.Buffer[..stream.Offset].ToArray()));
        }

        if (blockActors is { Count: > 0 }) {
            using BinaryStream stream = BinaryStream.Rent(64 * 1024);
            BinaryWriter writer = stream.GetWriter();
            if (actors is not null) {
                foreach (BlockActor actor in actors.Values) actor.OnWrite(actor.Nbt);
            }
            foreach (CompoundTag actor in blockActors.Values) NbtCodec.Write(actor, ref writer, Options.LittleEndian);
            entries.Add(new ChunkStorageEntry(ChunkStorageType.BlockActor, 0, stream.Buffer[..stream.Offset].ToArray()));
        }

        if (PendingTicks is { } pendingTicks) {
            int pendingSize = pendingTicks.GetSize(Options.LittleEndian);
            int existingSize = 0;
            foreach (ChunkStorageEntry entry in entries) existingSize = checked(existingSize + entry.Data.Length);
            if (pendingSize > WorldProviderService.MaximumChunkDataBytes - existingSize)
                throw new InvalidDataException("Pending ticks exceed the remaining chunk storage limit.");

            byte[] data = new byte[pendingSize];
            int offset = 0;
            NbtCodec.Write(pendingTicks, data, ref offset, Options.LittleEndian);
            entries.Add(new ChunkStorageEntry(ChunkStorageType.PendingTicks, 0, data));
        }

        return entries.ToArray();
    }

    public static Chunk Deserialize(int x, int z, ChunkStorageEntry[] entries, Dimension dimension) {
        Chunk chunk = new(x, z);
        int minimumSubChunkY = dimension.MinimumY >> 4;
        int subChunkCount = dimension.HeightRange >> 4;
        ChunkStorageEntry? data3D = null;
        ChunkStorageEntry? actors = null;
        ChunkStorageEntry? pendingTicks = null;
        foreach (ChunkStorageEntry entry in entries) {
            switch (entry.Type) {
                case ChunkStorageType.Data3D:
                    data3D = entry;
                    break;
                case ChunkStorageType.SubChunk: {
                        if (entry.SubChunkY < minimumSubChunkY || entry.SubChunkY >= minimumSubChunkY + subChunkCount) break;

                        int offset = 0;
                        BinaryReader reader = new(entry.Data, ref offset);
                        SubChunk subChunk = SubChunk.Deserialize(ref reader, ChunkSerializationMode.LevelDb);
                        if (reader.Remaining != 0 || subChunk.Index != entry.SubChunkY)
                            throw new InvalidDataException("The stored subchunk does not match its key.");
                        subChunk.Chunk = chunk;
                        if (chunk.subChunks is null) chunk.subChunks = [];
                        if (!chunk.subChunks.TryAdd(entry.SubChunkY, subChunk))
                            throw new InvalidDataException("The stored chunk contains a duplicate subchunk.");
                        break;
                    }
                case ChunkStorageType.BlockActor:
                    actors = entry;
                    break;
                case ChunkStorageType.PendingTicks:
                    if (pendingTicks is not null)
                        throw new InvalidDataException("The stored chunk contains duplicate pending tick records.");
                    pendingTicks = entry;
                    break;
            }
        }

        if (data3D is null) throw new InvalidDataException("The LevelDB chunk is missing its biome record.");

        int dataOffset = 0;
        BinaryReader dataReader = new(data3D.Value.Data, ref dataOffset);
        if (dataReader.Remaining < 512) throw new InvalidDataException("The LevelDB chunk heightmap is incomplete.");
        dataReader.Offset += 512;
        for (int subChunkY = minimumSubChunkY; subChunkY < minimumSubChunkY + subChunkCount; subChunkY++) {
            byte header = dataReader.ReadUInt8();
            if ((header >> 1) == 127) continue;
            dataReader.Offset--;
            BiomeStorage biomes = BiomeStorage.Deserialize(ref dataReader, ChunkSerializationMode.LevelDb, true)!;
            if (chunk.GetSubChunk(subChunkY) is { } subChunk) {
                subChunk.Biomes = biomes;
            } else if (!biomes.Empty) {
                chunk.GetOrCreateSubChunk(subChunkY).Biomes = biomes;
            }
        }
        if (dataReader.Remaining > 0) {
            // Vanilla can store one biome section above the build limit
            int upperOffset = dataReader.Offset;
            byte header = dataReader.ReadUInt8();
            if ((header >> 1) != 127) {
                dataReader.Offset--;
                BiomeStorage.Deserialize(ref dataReader, ChunkSerializationMode.LevelDb, true);
            }
            if (dataReader.Remaining != 0) throw new InvalidDataException("The LevelDB biome record has trailing data.");
            chunk.upperBiomeData = data3D.Value.Data.AsSpan(upperOffset).ToArray();
        }

        if (actors is { } actorEntry) {
            int actorOffset = 0;
            BinaryReader actorReader = new(actorEntry.Data, ref actorOffset);
            while (actorReader.Remaining > 0) {
                if (NbtCodec.Read(ref actorReader, Options.LittleEndian) is not CompoundTag actor ||
                    !actor.TryGet("x", out IntTag? actorX) ||
                    !actor.TryGet("y", out IntTag? actorY) ||
                    !actor.TryGet("z", out IntTag? actorZ))
                    throw new InvalidDataException("Stored block actor is missing its coordinates.");
                int localX = actorX!.Value - x * 16;
                int localZ = actorZ!.Value - z * 16;
                if ((uint)localX > 15 || (uint)localZ > 15) throw new InvalidDataException("Stored block actor is outside its chunk.");
                chunk.SetBlockNbt(localX, actorY!.Value, localZ, actor);
            }
        }

        if (pendingTicks is { } pendingEntry) {
            if (pendingEntry.Data.Length > WorldProviderService.MaximumChunkDataBytes)
                throw new InvalidDataException("The stored pending tick record exceeds the chunk storage limit.");
            int offset = 0;
            BinaryReader reader = new(pendingEntry.Data, ref offset);
            if (NbtCodec.Read(ref reader, Options.LittleEndian, PendingTickReadLimits) is not CompoundTag ticks || reader.Remaining != 0)
                throw new InvalidDataException("The stored pending tick record is invalid.");
            chunk.PendingTicks = ticks;
            if (ticks.Get<IntTag>("currentTick") is { Value: >= 0 } && ticks.Get<ListTag>("tickList") is { } list) {
                Dictionary<int, List<byte[]>> repeaterRecords = [];
                HashSet<Tag> duplicates = [];
                byte[] recordBuffer = [];
                for (int index = list.Value.Count - 1; index >= 0; index--) {
                    if (list.Value[index] is not CompoundTag record ||
                        record.Get<LongTag>("time") is not { Value: >= 0 } ||
                        record.Get<IntTag>("x") is not { } tickX ||
                        record.Get<IntTag>("y") is not { } tickY ||
                        record.Get<IntTag>("z") is not { } tickZ ||
                        record.Get<CompoundTag>("blockState") is not { } blockState ||
                        blockState.Get<StringTag>("name") is not { } name)
                        continue;

                    if (name.Value is not ("minecraft:powered_repeater" or "minecraft:unpowered_repeater") ||
                        (tickX.Value >> 4) != x || (tickZ.Value >> 4) != z ||
                        tickY.Value < dimension.MinimumY || tickY.Value > dimension.MaximumY ||
                        !BlockPalette.HasScheduledUpdates(name.Value))
                        continue;

                    int size = record.GetSize();
                    if (recordBuffer.Length < size) recordBuffer = new byte[size];
                    int written = 0;
                    NbtCodec.Write(record, recordBuffer, ref written);
                    HashCode hash = new();
                    hash.AddBytes(recordBuffer.AsSpan(0, written));
                    int key = hash.ToHashCode();
                    bool duplicate = false;
                    if (repeaterRecords.TryGetValue(key, out List<byte[]>? previousRecords)) {
                        foreach (byte[] previous in previousRecords) {
                            if (!recordBuffer.AsSpan(0, written).SequenceEqual(previous)) continue;
                            duplicate = true;
                            break;
                        }
                    }
                    if (duplicate) {
                        duplicates.Add(record);
                        continue;
                    }
                    if (BlockStorage.ReadPermutation(blockState) is null) continue;
                    if (previousRecords is null) {
                        previousRecords = [];
                        repeaterRecords.Add(key, previousRecords);
                    }
                    previousRecords.Add(recordBuffer.AsSpan(0, written).ToArray());
                }
                if (duplicates.Count > 0) {
                    list.Value.RemoveAll(duplicates.Contains);
                    chunk.PendingTicksRecovered = true;
                }
            }
        }

        chunk.Revision = 0;
        chunk.Dimension = dimension;
        return chunk;
    }
    static void SetCoordinate(CompoundTag actor, string name, int value) {
        if (actor.TryGet(name, out IntTag? tag)) {
            tag!.Value = value;
        } else {
            actor.Add(name, new IntTag { Value = value });
        }
    }
}
