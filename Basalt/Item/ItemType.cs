namespace Basalt.Core.Item;

using Basalt.Core.Blocks;
using Basalt.Core.Item.Components;
using Basalt.Core.Item.Traits;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;


/// <summary>
/// Catalog metadata for an item's creative menu placement.
/// </summary>
public sealed class ItemCatalog
{
    private static readonly Dictionary<string, int> CategoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["construction"] = 1,
        ["nature"] = 2,
        ["equipment"] = 3,
        ["items"] = 4
    };

    /// <summary>
    /// Numeric category id sent over the protocol.
    /// </summary>
    public int Category { get; }

    /// <summary>
    /// Group display name, or null if this item has no group.
    /// </summary>
    public string? GroupName { get; }

    /// <summary>
    /// Group icon identifier, or null if this item has no group.
    /// </summary>
    public string? GroupIcon { get; }

    public ItemCatalog(string categoryName, string? groupName, string? groupIcon)
    {
        Category = CategoryMap.TryGetValue(categoryName, out int id) ? id : 0;
        GroupName = string.IsNullOrEmpty(groupName) ? null : StripMinecraftPrefix(groupName);
        GroupIcon = string.IsNullOrEmpty(groupIcon) ? null : groupIcon;
    }

    private static string StripMinecraftPrefix(string value)
    {
        return value.StartsWith("minecraft:", StringComparison.Ordinal)
            ? value["minecraft:".Length..]
            : value;
    }
}

public sealed class ItemType
{
    private static readonly Dictionary<string, ItemType> Registry = new(StringComparer.Ordinal);
    private static readonly Dictionary<int, ItemType> NetworkRegistry = [];

    public string Identifier { get; }
    public int NetworkId { get; }
    public int MaxStackSize { get; }
    public bool IsComponentBased { get; }
    public int Version { get; }
    public IReadOnlyList<string> Tags { get; }
    public CompoundTag Properties { get; }
    public BlockType? BlockType { get; }
    public ItemTypeComponentCollection Components { get; }
    public ItemCatalog? Catalog { get; }
    public float AttackDamage { get; }
    public IReadOnlyDictionary<string, Type> Traits => _traits;

    private readonly Dictionary<string, Type> _traits = new(StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, ItemType> Types => Registry;
    public static ItemType Air => GetOrAir("minecraft:air");

    public ItemType(
        string identifier,
        int networkId,
        int maxStackSize,
        IEnumerable<string>? tags,
        bool isComponentBased,
        int version,
        CompoundTag? properties = null,
        ItemCatalog? catalog = null,
        BlockType? blockType = null)
    {
        Identifier = identifier;
        NetworkId = networkId;
        MaxStackSize = maxStackSize;
        IsComponentBased = isComponentBased;
        Version = version;
        Tags = tags is null ? [] : [.. tags];
        Properties = properties ?? new CompoundTag();
        Components = new ItemTypeComponentCollection(this, Properties);
        BlockType = blockType ?? BlockType.Get(identifier);
        Catalog = catalog;

        Registry[identifier] = this;
        NetworkRegistry[networkId] = this;
        ItemTraitRegistry.BindTraitsToType(this);
        AttackDamage = ResolveDamage(Tags);
    }

    public static ItemType? Get(string identifier)
    {
        return Registry.TryGetValue(identifier, out ItemType? type) ? type : null;
    }

    public static ItemType GetOrAir(string identifier)
    {
        return Get(identifier) ?? Get("minecraft:air") ?? new ItemType("minecraft:air", 0, 64, [], true, 1);
    }

    public static ItemType? GetByNetwork(int networkId)
    {
        return NetworkRegistry.TryGetValue(networkId, out ItemType? type) ? type : null;
    }

    public static List<ItemType> GetAll()
    {
        return [.. Registry.Values];
    }

    public void RegisterTrait(Type traitType, string identifier)
    {
        if (!typeof(ItemTrait).IsAssignableFrom(traitType) || traitType.IsAbstract)
        {
            return;
        }

        _traits.TryAdd(identifier, traitType);
    }

    public bool TryGetComponentProperties(string component, out CompoundTag properties)
    {
        return Components.TryGetComponentProperties(component, out properties);
    }

    public static void EnsureRegistryCapacity(int capacity)
    {
        Registry.EnsureCapacity(capacity);
        NetworkRegistry.EnsureCapacity(capacity);
    }

    public static LegacyItem ToNetworkStack(ItemType type, ushort stackSize = 1, uint metadata = 0)
    {
        int networkBlockId = 0;
        if (type.BlockType is not null && type.BlockType.Permutations.Count > 0)
        {
            networkBlockId = type.BlockType.Permutations[0].NetworkId;
        }

        return new LegacyItem
        {
            NetworkId = type.NetworkId,
            StackSize = stackSize,
            Metadata = unchecked((int)metadata),
            ItemStackId = null,
            NetworkBlockId = networkBlockId,
            ExtraData = new ItemInstanceUserData
            {
                Nbt = null,
                CanPlaceOn = [],
                CanDestroy = [],
                Ticking = null
            }
        };
    }

    private static readonly float[] SwordDamage = [4, 5, 5, 6, 4, 7, 8];
    private static readonly float[] AxeDamage = [3, 4, 4, 5, 3, 6, 7];
    private static readonly float[] PickaxeDamage = [2, 3, 3, 4, 2, 5, 6];
    private static readonly float[] ShovelDamage = [1, 2, 2, 3, 1, 4, 5];
    private static readonly float[] HoeDamage = [2, 3, 3, 4, 2, 5, 7];

    private static float ResolveDamage(IReadOnlyList<string> tags)
    {
        float[]? table = null;
        int tier = -1;

        for (int i = 0; i < tags.Count; i++)
        {
            switch (tags[i])
            {
                case "minecraft:is_sword": table = SwordDamage; break;
                case "minecraft:is_axe": table = AxeDamage; break;
                case "minecraft:is_pickaxe": table = PickaxeDamage; break;
                case "minecraft:is_shovel": table = ShovelDamage; break;
                case "minecraft:is_hoe": table = HoeDamage; break;
                case "minecraft:wooden_tier": tier = 0; break;
                case "minecraft:stone_tier": tier = 1; break;
                case "minecraft:copper_tier": tier = 2; break;
                case "minecraft:iron_tier": tier = 3; break;
                case "minecraft:golden_tier": tier = 4; break;
                case "minecraft:diamond_tier": tier = 5; break;
                case "minecraft:netherite_tier": tier = 6; break;
            }
        }

        if (table is null || tier < 0) return 1f;
        return table[tier];
    }
}






