namespace Basalt.Core.Entities.AI.Memory;

public sealed class MemoryStore {
    private readonly Dictionary<MemoryKey, MemoryValue> _values = [];
    private readonly HashSet<MemoryKey> _registered = [];

    public int Count => _values.Count;

    public void Register(MemoryKey key) {
        _registered.Add(key);
    }

    public MemoryStatus GetStatus(MemoryKey key) {
        return _values.ContainsKey(key)
            ? MemoryStatus.Present
            : _registered.Contains(key) ? MemoryStatus.Registered : MemoryStatus.Absent;
    }

    public bool Has(MemoryKey key) => _values.ContainsKey(key);

    public bool IsRegistered(MemoryKey key) => _registered.Contains(key);

    public void Set<T>(MemoryKey key, T value, ulong? expiresAt = null) {
        ArgumentNullException.ThrowIfNull(value);
        _registered.Add(key);
        _values[key] = new MemoryValue(value, expiresAt);
    }

    public bool TryRead<T>(MemoryKey key, out T? value) {
        if (_values.TryGetValue(key, out MemoryValue memory) && memory.Value is T typed) {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    public bool Remove(MemoryKey key) => _values.Remove(key);

    public void Expire(ulong currentTick) {
        foreach (MemoryKey key in _values.Keys.ToArray()) {
            if (_values[key].ExpiresAt is ulong expiresAt && currentTick >= expiresAt) {
                _values.Remove(key);
            }
        }
    }
}
