namespace BasaltBE.Basalt.Worlds.Providers;

public interface IWorldDataProvider {
    byte[]? ReadWorldData();
    void WriteWorldData(ReadOnlySpan<byte> data);
}
