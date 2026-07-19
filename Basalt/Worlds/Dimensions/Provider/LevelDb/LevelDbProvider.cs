using System.Buffers.Binary;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;
using Basalt.Core.Profiling;
using LevelDB;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;

namespace Basalt.Core.Worlds.Dimensions.Provider;

public sealed class LevelDbProvider : WorldProvider
{
    private readonly DB _database;
    private readonly ChunkStore _chunks;
    private readonly PlayerStore _players;
    public override string Identifier => "leveldb";

    public LevelDbProvider(string path)
    {
        Directory.CreateDirectory(path);
        Options options = new() { CreateIfMissing = true };
        _database = new DB(options, path);
        EntityStore entities = new(_database);
        _chunks = new ChunkStore(_database, entities);
        _players = new PlayerStore(_database);
    }

    public override bool HasChunk(DimensionType dimensionType, int x, int z)
    {
        return _chunks.Exists(dimensionType, x, z);
    }

    public override ChunkColumn? LoadChunk(DimensionType dimensionType, int x, int z)
    {
        using var __zone = Profiler.BeginZone("LevelDb.LoadChunk");
        return _chunks.Load(dimensionType, x, z);
    }

    public override void SaveChunk(ChunkColumn chunk)
    {
        using var __zone = Profiler.BeginZone("LevelDb.SaveChunk");
        using WriteBatch batch = new();
        _chunks.Save(batch, chunk);
        _database.Write(batch);
    }

    public override void DeleteChunk(DimensionType dimensionType, int x, int z)
    {
        using var __zone = Profiler.BeginZone("LevelDb.DeleteChunk");
        using WriteBatch batch = new();
        _chunks.Delete(batch, dimensionType, x, z);
        _database.Write(batch);
    }

    public override CompoundTag? LoadPlayerData(string xuid)
    {
        using var __zone = Profiler.BeginZone("LevelDb.LoadPlayerData");
        return _players.Load(xuid);
    }

    public override byte[]? GetRawPlayerData(string xuid)
    {
        return _players.GetRaw(xuid);
    }

    public override CompoundTag? LoadPlayerDataFromRaw(byte[] data)
    {
        return PlayerStore.LoadFromRaw(data);
    }

    public override void SavePlayerData(string xuid, CompoundTag data)
    {
        using var __zone = Profiler.BeginZone("LevelDb.SavePlayerData");
        _players.Save(xuid, data);
    }

    public override IReadOnlyList<string> ListPlayerXuids()
    {
        return _players.ListXuids();
    }

    public override void Dispose()
    {
        _database.Dispose();
    }

    public override Vec3f? LoadSpawnPosition(DimensionType dimensionType)
    {
        byte[] key = LevelDbKeyBuilder.BuildSpawnPositionKey(dimensionType);
        byte[]? data = _database.Get(key);
        if (data is not { Length: 12 })
        {
            return null;
        }

        float x = BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(0, 4));
        float y = BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(4, 4));
        float z = BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(8, 4));
        return new Vec3f(x, y, z);
    }

    public override void SaveSpawnPosition(DimensionType dimensionType, Vec3f position)
    {
        byte[] key = LevelDbKeyBuilder.BuildSpawnPositionKey(dimensionType);
        byte[] data = new byte[12];
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(0, 4), position.X);
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(4, 4), position.Y);
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(8, 4), position.Z);
        _database.Put(key, data);
    }
}







