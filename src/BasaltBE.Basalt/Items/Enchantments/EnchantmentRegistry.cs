using System.Collections.Frozen;

namespace BasaltBE.Basalt.Items.Enchantments;

public sealed class EnchantmentRegistry {
    private readonly Lock registryLock = new();
    private FrozenDictionary<string, Enchantment> enchantments = FrozenDictionary<string, Enchantment>.Empty;

    public IReadOnlyCollection<Enchantment> Values => Volatile.Read(ref enchantments).Values;

    public EnchantmentRegistry() {
        foreach (Enchantment enchantment in EnchantmentPalette.Enchantments)
            Register(enchantment);
    }

    public Enchantment? Find(string identifier) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return Volatile.Read(ref enchantments).GetValueOrDefault(identifier);
    }

    public Enchantment Get(string identifier) => Find(identifier)
        ?? throw new KeyNotFoundException($"Enchantment '{identifier}' was not found.");

    public Enchantment Register(Enchantment enchantment) {
        ArgumentNullException.ThrowIfNull(enchantment);
        if (!enchantment.Identifier.Contains(':'))
            throw new ArgumentException("Enchantment identifiers must be namespaced.", nameof(enchantment));

        lock (registryLock) {
            FrozenDictionary<string, Enchantment> current = Volatile.Read(ref enchantments);
            if (current.ContainsKey(enchantment.Identifier))
                throw new InvalidOperationException($"Enchantment '{enchantment.Identifier}' is already registered.");
            Dictionary<string, Enchantment> updated = new(current, StringComparer.Ordinal) {
                [enchantment.Identifier] = enchantment
            };
            Volatile.Write(ref enchantments, updated.ToFrozenDictionary(StringComparer.Ordinal));
        }
        return enchantment;
    }

    public bool Unregister(string identifier) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        if (EnchantmentPalette.Find(identifier) is not null)
            return false;

        lock (registryLock) {
            FrozenDictionary<string, Enchantment> current = Volatile.Read(ref enchantments);
            if (!current.ContainsKey(identifier))
                return false;

            Dictionary<string, Enchantment> updated = new(current, StringComparer.Ordinal);
            updated.Remove(identifier);
            Volatile.Write(ref enchantments, updated.ToFrozenDictionary(StringComparer.Ordinal));
            return true;
        }
    }
}
