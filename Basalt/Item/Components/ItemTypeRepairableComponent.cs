namespace Basalt.Core.Item.Components;

using Basalt.Protocol.Nbt;


/// <summary>
/// Represents the "minecraft:repairable" component that defines
/// which items can repair this item and how much durability they restore.
/// </summary>
public sealed class ItemTypeRepairableComponent : ItemTypeComponent {
    public new static string Identifier => "minecraft:repairable";

    public ItemTypeRepairableComponent(ItemType type, CompoundTag component) : base(type, component) {
    }

    /// <summary>
    /// Gets all repair item entries for this component.
    /// </summary>
    public RepairEntry[] GetRepairItems() {
        ListTag? repairList = Component.Get<ListTag>("repair_items")
                              ?? Component.Get<ListTag>("repairItems");

        if (repairList is null) {
            return [];
        }

        List<RepairEntry> entries = new(repairList.Values.Count);
        foreach (BaseTag tag in repairList.Values) {
            if (tag is not CompoundTag entry) {
                continue;
            }

            List<RepairItemTarget> items = ParseRepairTargets(entry);
            string repairAmount = ParseRepairAmount(entry);

            entries.Add(new RepairEntry([.. items], repairAmount));
        }

        return [.. entries];
    }

    private static List<RepairItemTarget> ParseRepairTargets(CompoundTag entry) {
        ListTag? itemsList = entry.Get<ListTag>("items");
        List<RepairItemTarget> items = [];

        if (itemsList is null) {
            return items;
        }

        foreach (BaseTag itemTag in itemsList.Values) {
            if (itemTag is StringTag stringItem) {
                items.Add(new RepairItemTarget(stringItem.Value, null));
            }
            else if (itemTag is CompoundTag compoundItem) {
                string? tagQuery = compoundItem.Get<StringTag>("tags")?.Value;
                items.Add(new RepairItemTarget(null, tagQuery));
            }
        }

        return items;
    }

    private static string ParseRepairAmount(CompoundTag entry) {
        if (entry.Get<IntTag>("repair_amount") is IntTag intAmount) {
            return intAmount.Value.ToString();
        }

        if (entry.Get<FloatTag>("repair_amount") is FloatTag floatAmount) {
            return floatAmount.Value.ToString();
        }

        if (entry.Get<StringTag>("repair_amount") is StringTag strAmount) {
            return strAmount.Value;
        }

        if (entry.Get<IntTag>("repairAmount") is IntTag intAmount2) {
            return intAmount2.Value.ToString();
        }

        if (entry.Get<FloatTag>("repairAmount") is FloatTag floatAmount2) {
            return floatAmount2.Value.ToString();
        }

        if (entry.Get<StringTag>("repairAmount") is StringTag strAmount2) {
            return strAmount2.Value;
        }

        return "0";
    }
}

/// <summary>
/// An item or tag query that can be used for repair.
/// </summary>
public readonly struct RepairItemTarget {
    /// <summary>
    /// The specific item identifier, or null if using a tag query.
    /// </summary>
    public string? Item { get; }

    /// <summary>
    /// The Molang tag query, or null if targeting a specific item.
    /// </summary>
    public string? TagQuery { get; }

    public RepairItemTarget(string? item, string? tagQuery) {
        Item = item;
        TagQuery = tagQuery;
    }
}

/// <summary>
/// A single repair entry defining which items repair and by how much.
/// </summary>
public readonly struct RepairEntry {
    /// <summary>
    /// The items or tag queries that can be used for repair.
    /// </summary>
    public RepairItemTarget[] Items { get; }

    /// <summary>
    /// The repair amount (numeric value or Molang expression like "q.max_durability * 0.25").
    /// </summary>
    public string RepairAmount { get; }

    public RepairEntry(RepairItemTarget[] items, string repairAmount) {
        Items = items;
        RepairAmount = repairAmount;
    }
}
