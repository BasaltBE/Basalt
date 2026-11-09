using BasaltBE.Basalt.Items.Types;

namespace BasaltBE.Basalt.Items;

public sealed class ItemRegistry {
    private readonly int ownerThread = Environment.CurrentManagedThreadId;

    public CustomItem Create(string identifier) {
        if (Environment.CurrentManagedThreadId != ownerThread)
            throw new InvalidOperationException("Create items on the server startup thread.");
        return ItemPalette.Create(identifier);
    }

    public ItemType? Find(string identifier) => ItemPalette.Find(identifier);

    public ItemType Get(string identifier) => Find(identifier)
        ?? throw new KeyNotFoundException($"Item '{identifier}' was not found.");
}
