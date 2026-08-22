using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Types;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;

namespace Basalt.Core.Worlds.Dimensions.Provider;

public abstract class WorldProvider : IDisposable {
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
    public abstract bool HasChunk(DimensionId dimensionType, int x, int z);
    public abstract ChunkColumn? LoadChunk(DimensionId dimensionType, int x, int z);
    public abstract void SaveChunk(ChunkColumn chunk);
    public abstract void DeleteChunk(DimensionId dimensionType, int x, int z);

    public virtual Vec3? LoadSpawnPosition(DimensionId dimensionType) {
        return null;
    }

    public virtual void SaveSpawnPosition(DimensionId dimensionType, Vec3 position) {
    }

    public virtual CompoundTag? LoadPlayerData(string xuid) {
        return null;
    }

    /// <summary>
    /// Gets the raw byte data for a player without deserializing.
    /// Useful for checking existence across multiple keys before committing to deserialization.
    /// </summary>
    public virtual byte[]? GetRawPlayerData(string xuid) {
        return null;
    }

    /// <summary>
    /// Deserializes player data from raw bytes previously obtained via <see cref="GetRawPlayerData"/>.
    /// </summary>
    public virtual CompoundTag? LoadPlayerDataFromRaw(byte[] data) {
        return null;
    }

    public virtual void SavePlayerData(string xuid, CompoundTag data) {
    }

    public virtual void DeletePlayerData(string xuid) {
    }

    public virtual void WriteLevelDat(World world) {
    }

    public virtual (long DayTime, ulong TickValue) LoadWorldTime() {
        return (0, 0);
    }

    public virtual IReadOnlyList<string> ListPlayerXuids() {
        return [];
    }

    public abstract void Dispose();

    protected static long HashChunk(int x, int z) {
        return ((long)x << 32) | (uint)z;
    }
}







