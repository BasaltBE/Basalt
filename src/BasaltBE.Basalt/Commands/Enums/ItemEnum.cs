using System.Collections.Frozen;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Types;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class ItemEnum() : IdentifierEnum<ItemType>("Item", Values) {
    private static readonly FrozenDictionary<string, ItemType> Values =
        Index(ItemPalette.Types, static type => type.Identifier);
}
