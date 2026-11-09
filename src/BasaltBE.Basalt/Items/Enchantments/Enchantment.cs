using BasaltBE.Basalt.Items.Types;

namespace BasaltBE.Basalt.Items.Enchantments;

public sealed class Enchantment {
    private readonly Predicate<ItemType> appliesTo;
    private readonly string[] incompatibleWith;

    public readonly string Identifier;
    public readonly int MaximumLevel;
    public readonly int AnvilCost;
    public ReadOnlyMemory<string> IncompatibleWith => incompatibleWith;
    internal readonly short? BedrockId;

    public Enchantment(
        string identifier,
        int maximumLevel,
        Predicate<ItemType> appliesTo,
        params string[] incompatibleWith
    ) : this(identifier, maximumLevel, appliesTo, null, 1, incompatibleWith) {
    }

    public Enchantment(
        string identifier,
        int maximumLevel,
        Predicate<ItemType> appliesTo,
        int anvilCost,
        params string[] incompatibleWith
    ) : this(identifier, maximumLevel, appliesTo, null, anvilCost, incompatibleWith) {
    }

    internal Enchantment(
        string identifier,
        int maximumLevel,
        Predicate<ItemType> appliesTo,
        short? bedrockId,
        int anvilCost,
        params string[] incompatibleWith
    ) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumLevel);
        ArgumentNullException.ThrowIfNull(appliesTo);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(anvilCost);
        ArgumentNullException.ThrowIfNull(incompatibleWith);
        Identifier = identifier;
        MaximumLevel = maximumLevel;
        AnvilCost = anvilCost;
        this.appliesTo = appliesTo;
        BedrockId = bedrockId;
        this.incompatibleWith = (string[])incompatibleWith.Clone();
    }

    public bool AppliesTo(ItemType item) => appliesTo(item);

    public bool IsIncompatibleWith(string identifier) =>
        Array.IndexOf(incompatibleWith, identifier) >= 0;
}
