namespace BasaltBE.Basalt.Entities;

public class Entity {
    private static long nextUniqueId = 0;
    private static long nextRuntimeId = 0;

    public long UniqueId = Interlocked.Increment(ref nextUniqueId);
    public ulong RuntimeId = checked((ulong)Interlocked.Increment(ref nextRuntimeId));
}
