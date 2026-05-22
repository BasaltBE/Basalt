using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using LevelDB;
using ChunkColumn = Basalt.World.Dimension.Chunk.Chunk;

namespace Basalt.World.Dimension.Provider;

public sealed class LevelDbProvider : WorldProvider
{
    private readonly DB _database;
    private readonly ChunkStore _chunks;
    private readonly PlayerStore _players;
    public override string Identifier => "leveldb";

    public LevelDbProvider(string path)
    {
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
        return _chunks.Load(dimensionType, x, z);
    }

    public override void SaveChunk(ChunkColumn chunk)
    {
        using WriteBatch batch = new();
        _chunks.Save(batch, chunk);
        _database.Write(batch);
    }

    public override void DeleteChunk(DimensionType dimensionType, int x, int z)
    {
        using WriteBatch batch = new();
        _chunks.Delete(batch, dimensionType, x, z);
        _database.Write(batch);
    }

    public override CompoundTag? LoadPlayerData(string xuid)
    {
        return _players.Load(xuid);
    }

    public override void SavePlayerData(string xuid, CompoundTag data)
    {
        _players.Save(xuid, data);
    }

    public override void Dispose()
    {
        _database.Dispose();
    }
}
