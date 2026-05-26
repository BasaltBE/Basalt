using Basalt.Item;

namespace Basalt.Commands;

public class ItemEnum : CommandEnum
{
    const string VanillaPrefix = "minecraft:";

    public string Raw = string.Empty;

    public ItemType Type = ItemType.Air;

    public ItemEnum() : base("Item")
    {
        ItemPalette.LoadVanilla();
        Options = [.. ItemType.Types.Keys.Select(TrimPrefix)];
    }

    public ItemEnum(string raw, ItemType type) : base("Item")
    {
        Raw = raw;
        Type = type;
    }

    static string TrimPrefix(string identifier)
    {
        if (!identifier.StartsWith(VanillaPrefix, StringComparison.Ordinal))
        {
            return identifier;
        }

        return identifier[VanillaPrefix.Length..];
    }
}
