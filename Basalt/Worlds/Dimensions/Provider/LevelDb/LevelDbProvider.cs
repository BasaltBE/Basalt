using System.Buffers.Binary;
using System.Text;
using BinaryReader = Basalt.Binary.BinaryReader;
using Basalt.Core.Player;
using Basalt.Core.Profiling;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;

using BedrockProtocol.Nbt;
using BedrockProtocol.Types;

namespace Basalt.Core.Worlds.Dimensions.Provider;

public sealed class LevelDbProvider : WorldProvider {
    private readonly LevelDbDatabase _database;
    private readonly ChunkStore _chunks;
    private readonly string _path;
    public override string Identifier => "leveldb";

    public string DatabasePath => _path;

    public LevelDbProvider(string path) {
        _path = ResolveDatabasePath(path);
        Directory.CreateDirectory(_path);
        _database = new LevelDbDatabase(_path);
        EntityStore entities = new(_database);
        _chunks = new ChunkStore(_database, entities);
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

    public override bool HasChunk(DimensionId dimensionType, int x, int z) {
        return _chunks.Exists(dimensionType, x, z);
    }

    public override ChunkColumn? LoadChunk(DimensionId dimensionType, int x, int z) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("LevelDb.LoadChunk") : default;
        return _chunks.Load(dimensionType, x, z);
    }

    public override void SaveChunk(ChunkColumn chunk) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("LevelDb.SaveChunk") : default;
        LevelDbWriteBatch batch = new();
        _chunks.Save(batch, chunk);
        _database.Write(batch);
    }

    public override void DeleteChunk(DimensionId dimensionType, int x, int z) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("LevelDb.DeleteChunk") : default;
        LevelDbWriteBatch batch = new();
        _chunks.Delete(batch, dimensionType, x, z);
        _database.Write(batch);
    }

    public override CompoundTag? LoadPlayerData(string xuid) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("LevelDb.LoadPlayerData") : default;
        byte[]? data = GetRawPlayerData(xuid);
        return data is null ? null : PlayerDataStore.Deserialize(data);
    }

    public override byte[]? GetRawPlayerData(string xuid) {
        if (string.IsNullOrWhiteSpace(xuid)) {
            return null;
        }

        byte[]? data = _database.Get(LevelDbKeyBuilder.BuildPlayerServerKey(xuid));
        return data is { Length: > 0 }
            ? data
            : _database.Get(LevelDbKeyBuilder.BuildLegacyPlayerStorageKey(xuid));
    }

    public override CompoundTag? LoadPlayerDataFromRaw(byte[] data) {
        return PlayerDataStore.Deserialize(data);
    }

    public override void SavePlayerData(string xuid, CompoundTag data) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("LevelDb.SavePlayerData") : default;
        if (string.IsNullOrWhiteSpace(xuid)) {
            throw new ArgumentException("Player xuid cannot be empty.", nameof(xuid));
        }

        _database.Put(LevelDbKeyBuilder.BuildPlayerServerKey(xuid), PlayerDataStore.Serialize(data));
        _database.Delete(LevelDbKeyBuilder.BuildLegacyPlayerStorageKey(xuid));
    }

    public override void DeletePlayerData(string xuid) {
        if (string.IsNullOrWhiteSpace(xuid)) {
            return;
        }

        _database.Delete(LevelDbKeyBuilder.BuildPlayerServerKey(xuid));
        _database.Delete(LevelDbKeyBuilder.BuildLegacyPlayerStorageKey(xuid));
    }

    public override IReadOnlyList<string> ListPlayerXuids() {
        List<string> xuids = [];
        using LevelDbIterator iterator = _database.CreateIterator();

        byte[] prefix = Encoding.UTF8.GetBytes("player_server_");
        iterator.Seek(prefix);

        while (iterator.Valid()) {
            ReadOnlySpan<byte> key = iterator.Key();
            if (key.Length <= prefix.Length || !key.StartsWith(prefix)) {
                break;
            }

            xuids.Add(Encoding.UTF8.GetString(key[prefix.Length..]));
            iterator.Next();
        }

        byte[] legacyPrefix = [0x35];
        iterator.Seek(legacyPrefix);

        while (iterator.Valid()) {
            ReadOnlySpan<byte> key = iterator.Key();
            if (key.Length == 0 || key[0] != 0x35) {
                break;
            }

            string xuid = Encoding.UTF8.GetString(key[1..]);
            if (!xuids.Contains(xuid)) {
                xuids.Add(xuid);
            }

            iterator.Next();
        }

        return xuids;
    }

    public override void Dispose() {
        _database.Dispose();
    }

    public override Vec3? LoadSpawnPosition(DimensionId dimensionType) {
        // Try legacy key first (for migration).
        byte[] legacyKey = LevelDbKeyBuilder.BuildLegacySpawnPositionKey(dimensionType);
        byte[]? data = _database.Get(legacyKey);
        if (data is { Length: 12 }) {
            float lx = BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(0, 4));
            float ly = BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(4, 4));
            float lz = BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(8, 4));
            return new Vec3() {
                X = lx,
                Y = ly,
                Z = lz,
            };
        }

        return null;
    }

    public override void SaveSpawnPosition(DimensionId dimensionType, Vec3 position) {
        byte[] data = new byte[12];
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(0, 4), position.X);
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(4, 4), position.Y);
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(8, 4), position.Z);
        _database.Put(LevelDbKeyBuilder.BuildLegacySpawnPositionKey(dimensionType), data);
    }

    public override void WriteLevelDat(World world) {
        string worldDir = Path.GetDirectoryName(_path) ?? _path;
        string levelDatPath = Path.Combine(worldDir, "level.dat");
        LevelDatWriter.Write(levelDatPath, world);

        // Write levelname.txt (vanilla expects this).
        string levelNamePath = Path.Combine(worldDir, "levelname.txt");
        File.WriteAllText(levelNamePath, world.Name);
    }

    public override (long DayTime, ulong TickValue) LoadWorldTime() {
        string worldDir = Path.GetDirectoryName(_path) ?? _path;
        string levelDatPath = Path.Combine(worldDir, "level.dat");
        if (!File.Exists(levelDatPath)) {
            return (0, 0);
        }

        try {
            byte[] file = File.ReadAllBytes(levelDatPath);
            if (file.Length <= 8) {
                return (0, 0);
            }

            int offset = 8;
            BinaryReader reader = new(file.AsSpan(8), ref offset);
            CompoundTag root = NBT.ReadTag<CompoundTag>(
                reader,
                new TagOptions(Name: true, Type: true, VarInt: false));
            long dayTime = root.Get<LongTag>("Time")?.Value ?? 0;
            long currentTick = root.Get<LongTag>("currentTick")?.Value ?? 0;
            return (Math.Max(0, dayTime), (ulong)Math.Max(0, currentTick));
        }
        catch {
            return (0, 0);
        }
    }
}







