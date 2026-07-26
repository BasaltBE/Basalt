using System.Buffers.Binary;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;
using Basalt.Core.Profiling;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;

namespace Basalt.Core.Worlds.Dimensions.Provider;

public sealed class LevelDbProvider : WorldProvider {
    private readonly LevelDbDatabase _database;
    private readonly ChunkStore _chunks;
    private readonly PlayerStore _players;
    private readonly string _path;
    public override string Identifier => "leveldb";

    public string DatabasePath => _path;

    public LevelDbProvider(string path) {
        _path = ResolveDatabasePath(path);
        Directory.CreateDirectory(_path);
        _database = new LevelDbDatabase(_path);
        EntityStore entities = new(_database);
        _chunks = new ChunkStore(_database, entities);
        _players = new PlayerStore(_database);
    }

    public static string ResolveDatabasePath(string path) {
        string normalizedPath = Path.TrimEndingDirectorySeparator(path);
        if (string.Equals(Path.GetFileName(normalizedPath), "db", StringComparison.OrdinalIgnoreCase)) {
            return normalizedPath;
        }

        string databasePath = Path.Combine(normalizedPath, "db");
        if (Directory.Exists(databasePath)) {
            return databasePath;
        }

        if (File.Exists(Path.Combine(normalizedPath, "CURRENT"))) {
            return normalizedPath;
        }

        return databasePath;
    }

    public override bool HasChunk(DimensionType dimensionType, int x, int z) {
        return _chunks.Exists(dimensionType, x, z);
    }

    public override ChunkColumn? LoadChunk(DimensionType dimensionType, int x, int z) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("LevelDb.LoadChunk") : default;
        return _chunks.Load(dimensionType, x, z);
    }

    public override void SaveChunk(ChunkColumn chunk) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("LevelDb.SaveChunk") : default;
        LevelDbWriteBatch batch = new();
        _chunks.Save(batch, chunk);
        _database.Write(batch);
    }

    public override void DeleteChunk(DimensionType dimensionType, int x, int z) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("LevelDb.DeleteChunk") : default;
        LevelDbWriteBatch batch = new();
        _chunks.Delete(batch, dimensionType, x, z);
        _database.Write(batch);
    }

    public override CompoundTag? LoadPlayerData(string xuid) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("LevelDb.LoadPlayerData") : default;
        return _players.Load(xuid);
    }

    public override byte[]? GetRawPlayerData(string xuid) {
        return _players.GetRaw(xuid);
    }

    public override CompoundTag? LoadPlayerDataFromRaw(byte[] data) {
        return PlayerStore.LoadFromRaw(data);
    }

    public override void SavePlayerData(string xuid, CompoundTag data) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("LevelDb.SavePlayerData") : default;
        _players.Save(xuid, data);
    }

    public override IReadOnlyList<string> ListPlayerXuids() {
        return _players.ListXuids();
    }

    public override void Dispose() {
        _database.Dispose();
    }

    public override Vec3f? LoadSpawnPosition(DimensionType dimensionType) {
        // Try legacy key first (for migration).
        byte[] legacyKey = LevelDbKeyBuilder.BuildLegacySpawnPositionKey(dimensionType);
        byte[]? data = _database.Get(legacyKey);
        if (data is { Length: 12 }) {
            float lx = BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(0, 4));
            float ly = BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(4, 4));
            float lz = BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(8, 4));
            return new Vec3f(lx, ly, lz);
        }

        return null;
    }

    public override void SaveSpawnPosition(DimensionType dimensionType, Vec3f position) {
        // Delete legacy key if it exists.
        _database.Delete(LevelDbKeyBuilder.BuildLegacySpawnPositionKey(dimensionType));
    }

    public override void WriteLevelDat(World world) {
        string worldDir = Path.GetDirectoryName(_path) ?? _path;
        string levelDatPath = Path.Combine(worldDir, "level.dat");
        LevelDatWriter.Write(levelDatPath, world);

        // Write levelname.txt (vanilla expects this).
        string levelNamePath = Path.Combine(worldDir, "levelname.txt");
        File.WriteAllText(levelNamePath, world.Name);
    }
}







