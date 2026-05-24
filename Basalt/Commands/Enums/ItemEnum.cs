using Basalt.Item;

namespace Basalt.Commands;

public class ItemEnum : CommandEnum
{
    public string Raw = string.Empty;

    public ItemType Type = ItemType.Air;

    public ItemEnum() : base("Item") { }

    public ItemEnum(string raw, ItemType type) : base("Item")
    {
        Raw = raw;
        Type = type;
    }
}
