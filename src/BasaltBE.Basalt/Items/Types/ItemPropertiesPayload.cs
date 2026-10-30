using System.Text.Json.Serialization;
using BasaltBE.Nbt.Enums;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Items.Types;

public sealed class ItemPropertiesPayload {
    public string[] Components = [];
    public string[] ItemTags = [];
    public bool Stackable;
    public int MaxAmount;
    [JsonInclude, JsonPropertyName("entityPlacer")]
    public Dictionary<string, string>? EntityPlacer;
    public Dictionary<string, Tag> Values = new(StringComparer.Ordinal);

    public CompoundTag ToComponentData() {
        if (Components.Length == 0 && ItemTags.Length == 0 && Values.Count == 0)
            return new CompoundTag();

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

        components.Add("item_tags", itemTags);

        CompoundTag data = new();
        data.Add("components", components);
        return data;
    }
}
