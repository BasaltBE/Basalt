using Basalt.Block;
using Basalt.Item.Components;
using Basalt.Item.Traits;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;

namespace Basalt.Item;

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
        CompoundTag? properties = null)
    {
        Identifier = identifier;
        NetworkId = networkId;
        MaxStackSize = maxStackSize;
        IsComponentBased = isComponentBased;
        Version = version;
        Tags = tags is null ? [] : [.. tags];
        Properties = properties ?? new CompoundTag();
        Components = new ItemTypeComponentCollection(this, Properties);
        BlockType = Block.BlockType.Get(identifier);

        Registry[identifier] = this;
        NetworkRegistry[networkId] = this;
        ItemTraitRegistry.BindTraitsToType(this);
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

    public static void EnsureRegistryCapacity(int capacity)
    {
        Registry.EnsureCapacity(capacity);
        NetworkRegistry.EnsureCapacity(capacity);
    }

    public static NetworkItemStackDescriptor ToNetworkStack(ItemType type, ushort stackSize = 1, uint metadata = 0)
    {
        int networkBlockId = 0;
        if (type.BlockType is not null && type.BlockType.Permutations.Count > 0)
        {
            networkBlockId = type.BlockType.Permutations[0].NetworkId;
        }

        return new NetworkItemStackDescriptor
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
}
