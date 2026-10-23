namespace BasaltBE.Basalt.Worlds.Providers.Types;

public readonly record struct WorldStorageMetrics(
    bool Enabled,
    int PendingOperations,
    long PendingBytes,
    long ReadCount,
    long WriteCount,
    double AverageReadMilliseconds,
    double AverageDecodeMilliseconds,
    double AverageWriteMilliseconds
);
