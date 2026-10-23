using System.Buffers.Binary;
using System.Text;
using BasaltBE.Basalt.Worlds.Providers.Types;
using LevelDB.Core.Api;
using LevelDB.Core.Db;
using LevelDB.Core.Util;
using LevelDB.MCPE;
using LevelDB.Platform;

namespace BasaltBE.Basalt.Worlds.Providers;

public sealed class LevelDbProvider : IWorldProvider {
    private const int MinimumSubChunkY = -4;
    private const int SubChunkCount = 24;
    private const byte BedrockChunkVersion = 42;
    private static readonly byte[] DimensionKeyPrefix = "BasaltBE.Dimension."u8.ToArray();
    private static readonly byte[] FinalizedChunkData = [2, 0, 0, 0];
    private static readonly byte[] ChunkMetadataHash = [0xA4, 0x10, 0xCE, 0xC8, 0x86, 0xD7, 0x1C, 0x4F];
    private static readonly byte[] ChunkBlendingData = [0, 11];
    private static readonly byte[] ChunkActorDigestVersion = [0];
    private readonly IDB database;
    private readonly ReadOptions readOptions = new();
    private readonly WriteOptions writeOptions = new() { Sync = true };

    public string Identifier => "leveldb";
    public string DatabasePath { get; }

    public LevelDbProvider(string path) {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        DatabasePath = Path.GetFullPath(path);
        Directory.CreateDirectory(DatabasePath);

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
        batch.Delete(new Slice(CreateKey(key, ChunkStorageType.Data3D)));
        batch.Delete(new Slice(CreateKey(key, ChunkStorageType.Version)));
        batch.Delete(new Slice(CreateKey(key, ChunkStorageType.FinalizedState)));
        batch.Delete(new Slice(CreateKey(key, ChunkStorageType.MetadataHash)));
        batch.Delete(new Slice(CreateKey(key, ChunkStorageType.BlendingData)));
        batch.Delete(new Slice(CreateKey(key, ChunkStorageType.ActorDigestVersion)));
        batch.Delete(new Slice(CreateKey(key, ChunkStorageType.BlockActor)));
        for (int subChunkY = MinimumSubChunkY; subChunkY < MinimumSubChunkY + SubChunkCount; subChunkY++)
            batch.Delete(new Slice(CreateKey(key, ChunkStorageType.SubChunk, checked((sbyte)subChunkY))));
        database.Write(writeOptions, batch).ThrowIfNotOk();
        return true;
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

    private static byte[] CreateKey(ChunkStorageKey key, ChunkStorageType type, sbyte subChunkY = 0) {
        if (!key.PrimaryStorageDimension) {
            byte[] identifier = Encoding.UTF8.GetBytes(key.DimensionIdentifier);
            if (identifier.Length > ushort.MaxValue)
                throw new InvalidDataException("Dimension identifiers cannot exceed 65535 UTF-8 bytes.");

            int keyLength = DimensionKeyPrefix.Length + sizeof(int) + sizeof(ushort) + identifier.Length + 2 * sizeof(int) + 1;
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
            customKey[customOffset++] = (byte)type;
            if (type == ChunkStorageType.SubChunk)
                customKey[customOffset] = unchecked((byte)subChunkY);
            return customKey;
        }

        int dimension = (int)key.DimensionType;
        byte[] databaseKey = new byte[dimension == 0 ? (type == ChunkStorageType.SubChunk ? 10 : 9) : (type == ChunkStorageType.SubChunk ? 14 : 13)];
        BinaryPrimitives.WriteInt32LittleEndian(databaseKey, key.X);
        BinaryPrimitives.WriteInt32LittleEndian(databaseKey.AsSpan(sizeof(int)), key.Z);
        int offset = 2 * sizeof(int);
        if (dimension != 0) {
            BinaryPrimitives.WriteInt32LittleEndian(databaseKey.AsSpan(offset), dimension);
            offset += sizeof(int);
        }
        databaseKey[offset++] = (byte)type;
        if (type == ChunkStorageType.SubChunk)
            databaseKey[offset] = unchecked((byte)subChunkY);
        return databaseKey;
    }
}
