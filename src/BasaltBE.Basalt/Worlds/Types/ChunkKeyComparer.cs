namespace BasaltBE.Basalt.Worlds.Types;

internal sealed class ChunkKeyComparer : IEqualityComparer<long> {
    internal static readonly ChunkKeyComparer Instance = new();

    public bool Equals(long x, long y) => x == y;

    public int GetHashCode(long value) {
        ulong hash = unchecked((ulong)value);
        hash ^= hash >> 30;
        hash *= 0xbf58476d1ce4e5b9UL;
        hash ^= hash >> 27;
        hash *= 0x94d049bb133111ebUL;
        return unchecked((int)(hash ^ (hash >> 31)));
    }
}
