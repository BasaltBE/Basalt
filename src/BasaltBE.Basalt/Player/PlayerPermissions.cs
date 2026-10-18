using System.Collections;
using BasaltBE.Basalt.Permissions;

namespace BasaltBE.Basalt.Entities;

public sealed class PlayerPermissions : IReadOnlyCollection<string> {
    private readonly Lock sync = new();
    private readonly PermissionStore? store;
    private string[] values = [];
    internal string[] Snapshot => Volatile.Read(ref values);
    internal bool Dirty;
    public int Count => Volatile.Read(ref values).Length;

    public PlayerPermissions() { }

    internal PlayerPermissions(PermissionStore store) {
        this.store = store;
    }

    public bool Contains(string permission) => Array.IndexOf(Volatile.Read(ref values), permission) >= 0;

    public bool Allows(string permission) {
        string[] snapshot = Volatile.Read(ref values);
        return permission.Length == 0 || Array.IndexOf(snapshot, "basalt.operator") >= 0 ||
            Array.IndexOf(snapshot, permission) >= 0;
    }

    public bool Add(string permission) {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        lock (sync) {
            if (Array.IndexOf(values, permission) >= 0)
                return false;
            Volatile.Write(ref values, [.. values, permission]);
            Volatile.Write(ref Dirty, true);
            if (store is not null)
                Interlocked.Increment(ref store.Revision);
            return true;
        }
    }

    public bool Remove(string permission) {
        lock (sync) {
            int index = Array.IndexOf(values, permission);
            if (index < 0)
                return false;
            string[] reduced = new string[values.Length - 1];
            values.AsSpan(0, index).CopyTo(reduced);
            values.AsSpan(index + 1).CopyTo(reduced.AsSpan(index));
            Volatile.Write(ref values, reduced);
            Volatile.Write(ref Dirty, true);
            if (store is not null)
                Interlocked.Increment(ref store.Revision);
            return true;
        }
    }

    public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)Volatile.Read(ref values)).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
