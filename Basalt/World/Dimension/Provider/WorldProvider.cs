using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using ChunkColumn = Basalt.World.Dimension.Chunk.Chunk;

namespace Basalt.World.Dimension.Provider;

public abstract class WorldProvider : IDisposable
{   
    /// <summary>
    /// A unique identifier for the provider, used for saving and loading dimensions.
    /// </summary>
    public abstract string Identifier { get; }

    /// <summary>
    /// Checks if a chunk exists in the provider.
    /// </summary>
    /// <param name="dimensionType"></param>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public abstract bool HasChunk(DimensionType dimensionType, int x, int z);
    public abstract ChunkColumn? LoadChunk(DimensionType dimensionType, int x, int z);
    public abstract void SaveChunk(ChunkColumn chunk);
    public abstract void DeleteChunk(DimensionType dimensionType, int x, int z);
    public virtual CompoundTag? LoadPlayerData(string xuid)
    {
        return null;
    }

    public virtual void SavePlayerData(string xuid, CompoundTag data)
    {
    }

    public virtual IReadOnlyList<string> ListPlayerXuids()
    {
        return [];
    }

    public abstract void Dispose();

    protected static long HashChunk(int x, int z)
    {
        return ((long)x << 32) | (uint)z;
    }
}
