using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using ChunkColumn = Basalt.World.Dimension.Chunk.Chunk;

namespace Basalt.World.Dimension.Provider;

public abstract class WorldProvider : IDisposable
{
    public abstract string Identifier { get; }
    public abstract bool HasChunk(int x, int z);
    public abstract ChunkColumn? LoadChunk(DimensionType dimensionType, int x, int z);
    public abstract void SaveChunk(ChunkColumn chunk);
    public abstract void DeleteChunk(int x, int z);
    public virtual CompoundTag? LoadPlayerData(string xuid)
    {
        return null;
    }

    public virtual void SavePlayerData(string xuid, CompoundTag data)
    {
    }

    public abstract void Dispose();

    protected static long HashChunk(int x, int z)
    {
        return ((long)x << 32) | (uint)z;
    }
}
