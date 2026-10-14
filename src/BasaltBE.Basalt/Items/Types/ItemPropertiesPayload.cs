using BasaltBE.Nbt.Enums;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Items.Types;

public sealed class ItemPropertiesPayload {
    public string[] Components = [];
    public string[] ItemTags = [];
    public bool Stackable;
    public int MaxAmount;
    public Dictionary<string, Tag> Values = new(StringComparer.Ordinal);

    public CompoundTag ToComponentData() {
        CompoundTag components = new();
        if (Values.TryGetValue("item_properties", out Tag? itemProperties))
            components.Add("item_properties", itemProperties);

        foreach (string identifier in Components) {
            if (identifier == "item_properties")
                continue;

            components.Add(identifier, Values.GetValueOrDefault(identifier) ?? new CompoundTag());
        }

        ListTag itemTags = new() { ElementType = TagType.String };
        foreach (string itemTag in ItemTags)
            itemTags.Value.Add(new StringTag { Value = itemTag });

        CompoundTag data = new();
        data.Add("components", components);
        data.Add("item_tags", itemTags);
        return data;
    }
}
