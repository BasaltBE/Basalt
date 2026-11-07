using System.Buffers.Binary;
using System.Text;
using BasaltBE.Basalt.Worlds.Providers.Types;
using LevelDB.Core.Api;
using LevelDB.Core.Db;
using LevelDB.Core.Util;
using LevelDB.MCPE;
using LevelDB.Platform;

namespace BasaltBE.Basalt.Worlds.Providers;

public sealed class LevelDbProvider : IWorldProvider, IWorldDataProvider, IEntityProvider {
    private const int MinimumSubChunkY = -32;
    private const int SubChunkCount = 64;
    private const byte BedrockChunkVersion = 42;
    private static readonly byte[] DimensionKeyPrefix = "BasaltBE.Dimension."u8.ToArray();
    private static readonly byte[] ActorCounterKey = "ActorIdCounter"u8.ToArray();
    private static readonly byte[] LegacyWorldDataKey = "BasaltBE.WorldData"u8.ToArray();
    private static readonly byte[] FinalizedChunkData = [2, 0, 0, 0];
    private static readonly byte[] ChunkMetadataHash = [0xA4, 0x10, 0xCE, 0xC8, 0x86, 0xD7, 0x1C, 0x4F];
    private static readonly byte[] ChunkBlendingData = [0, 11];
    private static readonly byte[] ChunkActorDigestVersion = [0];
    private readonly IDB database;
    private readonly ReadOptions readOptions = new();
    private readonly WriteOptions writeOptions = new() { Sync = true };
    private readonly string levelDatPath;
    private int levelDatVersion = 10;

    public string Identifier => "leveldb";
    public string DatabasePath { get; }

    public LevelDbProvider(string path) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        DatabasePath = Path.GetFullPath(path);
        Directory.CreateDirectory(DatabasePath);
        levelDatPath = Path.Combine(Directory.GetParent(DatabasePath)!.FullName, "level.dat");

        Options options = McpeOptions.Create(new Options { CreateIfMissing = true });
        DB.Open(options, DatabasePath, out IDB? opened).ThrowIfNotOk();
        database = opened ?? throw new InvalidOperationException($"Could not open LevelDB database at '{DatabasePath}'.");
    }

    public ChunkStorageEntry[]? ReadChunk(ChunkStorageKey key) {
        byte[] versionKey = CreateKey(key, ChunkStorageType.Version);
        Status status = database.Get(readOptions, new Slice(versionKey), out Slice versionData);
        if (status.IsNotFound)
            return null;
        status.ThrowIfNotOk();
        byte[] versionBytes = versionData.ToArray();
        if (versionBytes.Length != 1 || versionBytes[0] != BedrockChunkVersion)
            throw new InvalidDataException("The LevelDB chunk version is unsupported.");

        List<ChunkStorageEntry> entries = new(SubChunkCount + 7) {
            new(ChunkStorageType.Data3D, 0, ReadRecord(key, ChunkStorageType.Data3D) ?? []),
            new(ChunkStorageType.Version, 0, versionBytes),
            new(ChunkStorageType.FinalizedState, 0, ReadRecord(key, ChunkStorageType.FinalizedState) ?? []),
            new(ChunkStorageType.MetadataHash, 0, ReadRecord(key, ChunkStorageType.MetadataHash) ?? []),
            new(ChunkStorageType.BlendingData, 0, ReadRecord(key, ChunkStorageType.BlendingData) ?? []),
            new(ChunkStorageType.ActorDigestVersion, 0, ReadRecord(key, ChunkStorageType.ActorDigestVersion) ?? [])
        };

        for (int subChunkY = MinimumSubChunkY; subChunkY < MinimumSubChunkY + SubChunkCount; subChunkY++) {
            byte[]? data = ReadRecord(key, ChunkStorageType.SubChunk, checked((sbyte)subChunkY));
            if (data is not null)
                entries.Add(new ChunkStorageEntry(ChunkStorageType.SubChunk, checked((sbyte)subChunkY), data));
        }

        byte[]? actors = ReadRecord(key, ChunkStorageType.BlockActor);
        if (actors is not null)
            entries.Add(new ChunkStorageEntry(ChunkStorageType.BlockActor, 0, actors));
        byte[]? pendingTicks = ReadRecord(key, ChunkStorageType.PendingTicks);
        if (pendingTicks is not null)
            entries.Add(new ChunkStorageEntry(ChunkStorageType.PendingTicks, 0, pendingTicks));
        return entries.ToArray();
    }

    public void WriteChunks(ReadOnlySpan<ChunkStorageRecord> chunks) {
        if (chunks.IsEmpty)
            return;

        WriteBatch batch = new();
        foreach (ChunkStorageRecord chunk in chunks) {
            batch.Put(new Slice(CreateKey(chunk.Key, ChunkStorageType.Version)), new Slice([BedrockChunkVersion]));
            batch.Put(new Slice(CreateKey(chunk.Key, ChunkStorageType.FinalizedState)), new Slice(FinalizedChunkData));
            batch.Put(new Slice(CreateKey(chunk.Key, ChunkStorageType.MetadataHash)), new Slice(ChunkMetadataHash));
            batch.Put(new Slice(CreateKey(chunk.Key, ChunkStorageType.BlendingData)), new Slice(ChunkBlendingData));
            batch.Put(new Slice(CreateKey(chunk.Key, ChunkStorageType.ActorDigestVersion)), new Slice(ChunkActorDigestVersion));
            batch.Delete(new Slice(CreateKey(chunk.Key, ChunkStorageType.BlockActor)));
            batch.Delete(new Slice(CreateKey(chunk.Key, ChunkStorageType.PendingTicks)));
            for (int subChunkY = MinimumSubChunkY; subChunkY < MinimumSubChunkY + SubChunkCount; subChunkY++)
                batch.Delete(new Slice(CreateKey(chunk.Key, ChunkStorageType.SubChunk, checked((sbyte)subChunkY))));
            foreach (ChunkStorageEntry entry in chunk.Entries) {
                byte[] key = CreateKey(chunk.Key, entry.Type, entry.SubChunkY);
                batch.Put(new Slice(key), new Slice(entry.Data));
            }
        }

        database.Write(writeOptions, batch).ThrowIfNotOk();
    }

    public bool DeleteChunk(ChunkStorageKey key) {
        WriteBatch batch = new();
        byte[] actors = ReadActorDigest(key);
        for (int offset = 0; offset < actors.Length; offset += sizeof(long)) {
            long id = BinaryPrimitives.ReadInt64LittleEndian(actors.AsSpan(offset));
            batch.Delete(new Slice(CreateActorKey(id)));
        }

        batch.Delete(new Slice(CreateDigestKey(key)));
        batch.Delete(new Slice(CreateKey(key, ChunkStorageType.Data3D)));
        batch.Delete(new Slice(CreateKey(key, ChunkStorageType.Version)));
        batch.Delete(new Slice(CreateKey(key, ChunkStorageType.FinalizedState)));
        batch.Delete(new Slice(CreateKey(key, ChunkStorageType.MetadataHash)));
        batch.Delete(new Slice(CreateKey(key, ChunkStorageType.BlendingData)));
        batch.Delete(new Slice(CreateKey(key, ChunkStorageType.ActorDigestVersion)));
        batch.Delete(new Slice(CreateKey(key, ChunkStorageType.BlockActor)));
        batch.Delete(new Slice(CreateKey(key, ChunkStorageType.PendingTicks)));
        for (int subChunkY = MinimumSubChunkY; subChunkY < MinimumSubChunkY + SubChunkCount; subChunkY++)
            batch.Delete(new Slice(CreateKey(key, ChunkStorageType.SubChunk, checked((sbyte)subChunkY))));
        database.Write(writeOptions, batch).ThrowIfNotOk();
        return true;
    }

    public long ReserveEntityIds(long count) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        long lastId = ReadEntityIdCounter();
        byte[] counter = new byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(counter, checked(lastId + count));
        database.Put(writeOptions, new Slice(ActorCounterKey), new Slice(counter)).ThrowIfNotOk();
        return checked(lastId + 1);
    }

    private long ReadEntityIdCounter() {
        Status status = database.Get(readOptions, new Slice(ActorCounterKey), out Slice counter);
        if (!status.IsNotFound) {
            status.ThrowIfNotOk();
            byte[] bytes = counter.ToArray();
            if (bytes.Length != sizeof(long))
                throw new InvalidDataException("Actor ID counter length is invalid.");

            long id = BinaryPrimitives.ReadInt64LittleEndian(bytes);
            if (id < 0)
                throw new InvalidDataException("Actor ID counter is invalid.");
            return id;
        }

        long lastId = 0;
        byte[] prefix = "actorprefix"u8.ToArray();
        using IIterator iterator = database.NewIterator(readOptions);
        iterator.Seek(new Slice(prefix));
        while (iterator.Valid) {
            byte[] key = iterator.Key.ToArray();
            if (!key.AsSpan().StartsWith(prefix))
                break;
            if (key.Length != prefix.Length + sizeof(long))
                throw new InvalidDataException("Actor storage key length is invalid.");

            long id = BinaryPrimitives.ReadInt64LittleEndian(key.AsSpan(prefix.Length));
            lastId = Math.Max(lastId, id);
            iterator.Next();
        }
        iterator.Status.ThrowIfNotOk();
        return lastId;
    }

    public EntityStorageRecord[] ReadEntities(ChunkStorageKey key) {
        byte[] ids = ReadActorDigest(key, WorldProviderService.MaximumChunkDataBytes);
        List<EntityStorageRecord> records = new(ids.Length / sizeof(long));
        int totalBytes = 0;
        for (int offset = 0; offset < ids.Length; offset += sizeof(long)) {
            long id = BinaryPrimitives.ReadInt64LittleEndian(ids.AsSpan(offset));
            Status status = database.Get(readOptions, new Slice(CreateActorKey(id)), out Slice data);
            if (status.IsNotFound)
                continue;
            status.ThrowIfNotOk();

            byte[] bytes = data.ToArray();
            totalBytes = checked(totalBytes + bytes.Length);
            if (bytes.Length > 1024 * 1024 || totalBytes > WorldProviderService.MaximumChunkDataBytes)
                throw new InvalidDataException("Stored actors exceed the chunk read limit.");
            records.Add(new EntityStorageRecord(id, bytes));
        }
        return records.ToArray();
    }

    public void WriteEntities(ReadOnlySpan<EntityStorageChange> changes) {
        if (changes.IsEmpty)
            return;

        WriteBatch batch = new();
        Dictionary<ChunkStorageKey, HashSet<long>> digests = [];
        foreach (EntityStorageChange change in changes) {
            if ((change.Current is null) != (change.Data is null))
                throw new ArgumentException("Actor data and destination must both be present or absent.", nameof(changes));

            byte[] actorKey = CreateActorKey(change.Id);
            if (change.Previous is null && change.Data is { } created) {
                Status status = database.Get(readOptions, new Slice(actorKey), out Slice existing);
                if (!status.IsNotFound) {
                    status.ThrowIfNotOk();
                    if (!existing.ToArray().AsSpan().SequenceEqual(created))
                        throw new InvalidDataException($"Actor storage identity {change.Id} is already in use.");
                }
            }

            if (change.Previous is { } previous)
                Digest(previous).Remove(change.Id);

            if (change.Current is { } current && change.Data is { } data) {
                Digest(current).Add(change.Id);
                batch.Put(new Slice(actorKey), new Slice(data));
            }
            else {
                batch.Delete(new Slice(actorKey));
            }
        }

        foreach ((ChunkStorageKey key, HashSet<long> ids) in digests) {
            byte[] digestKey = CreateDigestKey(key);
            if (ids.Count == 0) {
                batch.Delete(new Slice(digestKey));
                continue;
            }

            byte[] bytes = new byte[checked(ids.Count * sizeof(long))];
            int offset = 0;
            foreach (long id in ids) {
                BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(offset), id);
                offset += sizeof(long);
            }
            batch.Put(new Slice(digestKey), new Slice(bytes));
        }

        database.Write(writeOptions, batch).ThrowIfNotOk();

        HashSet<long> Digest(ChunkStorageKey key) {
            if (digests.TryGetValue(key, out HashSet<long>? ids))
                return ids;

            byte[] bytes = ReadActorDigest(key, WorldProviderService.MaximumChunkDataBytes);
            ids = new HashSet<long>(bytes.Length / sizeof(long));
            for (int offset = 0; offset < bytes.Length; offset += sizeof(long))
                ids.Add(BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(offset)));
            digests.Add(key, ids);
            return ids;
        }
    }

    private byte[] ReadActorDigest(ChunkStorageKey key, int maximumBytes = int.MaxValue) {
        Status status = database.Get(readOptions, new Slice(CreateDigestKey(key)), out Slice digest);
        if (status.IsNotFound)
            return [];

        status.ThrowIfNotOk();
        byte[] bytes = digest.ToArray();
        if (bytes.Length % sizeof(long) != 0 || bytes.Length > maximumBytes)
            throw new InvalidDataException("Actor digest length is invalid.");
        return bytes;
    }

    private static byte[] CreateActorKey(long id) {
        ReadOnlySpan<byte> prefix = "actorprefix"u8;
        byte[] key = new byte[prefix.Length + sizeof(long)];
        prefix.CopyTo(key);
        BinaryPrimitives.WriteInt64LittleEndian(key.AsSpan(prefix.Length), id);
        return key;
    }

    private static byte[] CreateDigestKey(ChunkStorageKey chunk) {
        ReadOnlySpan<byte> prefix = "digp"u8;
        byte[] chunkKey = CreateKey(chunk);
        byte[] key = new byte[prefix.Length + chunkKey.Length];
        prefix.CopyTo(key);
        chunkKey.CopyTo(key, prefix.Length);
        return key;
    }

    public byte[]? ReadWorldData() {
        string path = File.Exists(levelDatPath) ? levelDatPath : levelDatPath + "_old";
        if (!File.Exists(path)) {
            Status status = database.Get(readOptions, new Slice(LegacyWorldDataKey), out Slice legacyData);
            if (status.IsNotFound)
                return null;

            status.ThrowIfNotOk();
            return legacyData.ToArray();
        }

        byte[] file = File.ReadAllBytes(path);
        if (file.Length < 8)
            throw new InvalidDataException("The level.dat header is incomplete.");

        levelDatVersion = BinaryPrimitives.ReadInt32LittleEndian(file);
        int dataLength = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(sizeof(int)));
        if (levelDatVersion < 1 || dataLength != file.Length - 8)
            throw new InvalidDataException("The level.dat header or payload length is invalid.");

        return file.AsSpan(8).ToArray();
    }

    public void WriteWorldData(ReadOnlySpan<byte> data) {
        byte[] file = new byte[checked(sizeof(int) * 2 + data.Length)];
        BinaryPrimitives.WriteInt32LittleEndian(file, levelDatVersion);
        BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(sizeof(int)), data.Length);
        data.CopyTo(file.AsSpan(sizeof(int) * 2));

        string temporaryPath = levelDatPath + "_new";
        string oldPath = levelDatPath + "_old";
        File.WriteAllBytes(temporaryPath, file);
        if (File.Exists(oldPath))
            File.Delete(oldPath);
        if (File.Exists(levelDatPath))
            File.Move(levelDatPath, oldPath);
        File.Move(temporaryPath, levelDatPath);

        WriteBatch batch = new();
        batch.Delete(new Slice(LegacyWorldDataKey));
        database.Write(writeOptions, batch).ThrowIfNotOk();
    }

    public void Flush() { }

    public void Dispose() => database.Dispose();

    private byte[]? ReadRecord(ChunkStorageKey key, ChunkStorageType type, sbyte subChunkY = 0) {
        Status status = database.Get(readOptions, new Slice(CreateKey(key, type, subChunkY)), out Slice value);
        if (status.IsNotFound)
            return null;

        status.ThrowIfNotOk();
        return value.ToArray();
    }

    private static byte[] CreateKey(ChunkStorageKey key, ChunkStorageType? type = null, sbyte subChunkY = 0) {
        if (!key.PrimaryStorageDimension) {
            byte[] identifier = Encoding.UTF8.GetBytes(key.DimensionIdentifier);
            if (identifier.Length > ushort.MaxValue)
                throw new InvalidDataException("Dimension identifiers cannot exceed 65535 UTF-8 bytes.");

            int keyLength = DimensionKeyPrefix.Length + sizeof(int) + sizeof(ushort) + identifier.Length + 2 * sizeof(int) + (type.HasValue ? 1 : 0);
            if (type == ChunkStorageType.SubChunk)
                keyLength++;

            byte[] customKey = new byte[keyLength];
            DimensionKeyPrefix.CopyTo(customKey, 0);
            int customOffset = DimensionKeyPrefix.Length;
            BinaryPrimitives.WriteInt32LittleEndian(customKey.AsSpan(customOffset), (int)key.DimensionType);
            customOffset += sizeof(int);
            BinaryPrimitives.WriteUInt16LittleEndian(customKey.AsSpan(customOffset), (ushort)identifier.Length);
            customOffset += sizeof(ushort);
            identifier.CopyTo(customKey, customOffset);
            customOffset += identifier.Length;
            BinaryPrimitives.WriteInt32LittleEndian(customKey.AsSpan(customOffset), key.X);
            customOffset += sizeof(int);
            BinaryPrimitives.WriteInt32LittleEndian(customKey.AsSpan(customOffset), key.Z);
            customOffset += sizeof(int);
            if (type.HasValue)
                customKey[customOffset++] = (byte)type.Value;
            if (type == ChunkStorageType.SubChunk)
                customKey[customOffset] = unchecked((byte)subChunkY);
            return customKey;
        }

        int dimension = (int)key.DimensionType;
        int length = 2 * sizeof(int);
        if (dimension != 0)
            length += sizeof(int);
        if (type.HasValue)
            length++;
        if (type == ChunkStorageType.SubChunk)
            length++;
        byte[] databaseKey = new byte[length];
        BinaryPrimitives.WriteInt32LittleEndian(databaseKey, key.X);
        BinaryPrimitives.WriteInt32LittleEndian(databaseKey.AsSpan(sizeof(int)), key.Z);
        int offset = 2 * sizeof(int);
        if (dimension != 0) {
            BinaryPrimitives.WriteInt32LittleEndian(databaseKey.AsSpan(offset), dimension);
            offset += sizeof(int);
        }
        if (type.HasValue)
            databaseKey[offset++] = (byte)type.Value;
        if (type == ChunkStorageType.SubChunk)
            databaseKey[offset] = unchecked((byte)subChunkY);
        return databaseKey;
    }
}
