namespace Basalt.Core.Item.Components;

using Basalt.Protocol.Nbt;


public sealed class ItemTypeSeedComponent : ItemTypeComponent
{
    public new static string Identifier => "minecraft:seed";

    public ItemTypeSeedComponent(ItemType type, CompoundTag component) : base(type, component)
    {
    }

    public string GetCropResult()
    {
        return Component.Get<StringTag>("cropResult")?.Value
               ?? Component.Get<StringTag>("crop_result")?.Value
               ?? string.Empty;
    }

    public string[] GetPlantAt()
    {
        if (Component.Get<ListTag>("plantAt") is ListTag list)
        {
            List<string> results = [];
            foreach (BaseTag tag in list.Values)
            {
                if (tag is StringTag st && !string.IsNullOrWhiteSpace(st.Value))
                {
                    results.Add(st.Value);
                }
            }
            return [.. results];
        }

        if (Component.Get<ListTag>("plant_at") is ListTag list2)
        {
            List<string> results = [];
            foreach (BaseTag tag in list2.Values)
            {
                if (tag is StringTag st && !string.IsNullOrWhiteSpace(st.Value))
                {
                    results.Add(st.Value);
                }
            }
            return [.. results];
        }

        return [];
    }
}
