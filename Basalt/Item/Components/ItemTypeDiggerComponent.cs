namespace Basalt.Core.Item.Components;

using BedrockProtocol.Nbt;


/// <summary>
/// Represents the "minecraft:digger" component that defines block destroy speeds.
/// </summary>
public sealed class ItemTypeDiggerComponent : ItemTypeComponent {
    public new static string Identifier => "minecraft:digger";

    public ItemTypeDiggerComponent(ItemType type, CompoundTag component) : base(type, component) {
    }

    public bool UseEfficiency() {
        return (Component.Get<ByteTag>("use_efficiency")?.Value
                ?? Component.Get<ByteTag>("useEfficiency")?.Value
                ?? 0) != 0;
    }

    public DestroySpeedEntry[] GetDestroySpeeds() {
        ListTag? speedsList = Component.Get<ListTag>("destroy_speeds")
                              ?? Component.Get<ListTag>("destroySpeeds");

        if (speedsList is null) {
            return [];
        }

        List<DestroySpeedEntry> entries = new(speedsList.Values.Count);
        foreach (BaseTag tag in speedsList.Values) {
            if (tag is not CompoundTag entry) {
                continue;
            }

            float speed = entry.Get<FloatTag>("speed")?.Value
                          ?? entry.Get<IntTag>("speed")?.Value
                          ?? 0f;

            string? blockId = null;
            string? tagQuery = null;

            if (entry.Get<StringTag>("block") is StringTag blockTag) {
                blockId = blockTag.Value;
            }
            else if (entry.Get<CompoundTag>("block") is CompoundTag blockCompound) {
                tagQuery = blockCompound.Get<StringTag>("tags")?.Value;
            }

            entries.Add(new DestroySpeedEntry(blockId, tagQuery, speed));
        }

        return [.. entries];
    }
}

/// <summary>
/// A single entry in the digger's destroy speeds list.
/// </summary>
public readonly struct DestroySpeedEntry {
    /// <summary>
    /// The specific block identifier, or null if using a tag query.
    /// </summary>
    public string? Block { get; }

    /// <summary>
    /// The Molang tag query, or null if targeting a specific block.
    /// </summary>
    public string? TagQuery { get; }

    /// <summary>
    /// The mining speed for matched blocks.
    /// </summary>
    public float Speed { get; }

    public DestroySpeedEntry(string? block, string? tagQuery, float speed) {
        Block = block;
        TagQuery = tagQuery;
        Speed = speed;
    }
}
