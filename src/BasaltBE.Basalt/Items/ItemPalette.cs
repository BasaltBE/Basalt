using System.Reflection;
using Basalt.Binary;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.Items;

public static class ItemPalette {
    public static readonly ItemType[] Types = LoadTypes();
    static readonly Dictionary<string, ItemType> ByIdentifier = Types.ToDictionary(type => type.Identifier, StringComparer.Ordinal);
    static readonly Dictionary<int, ItemType> ByNetworkId = Types.ToDictionary(type => type.NetworkId);
    internal static readonly ItemRegistryPacket RegistryPacket = CreateRegistryPacket();
    internal static readonly byte[] SerializedRegistry = SerializeRegistry(RegistryPacket);
    internal static readonly CreativeContentPacket CreativeContentPacket = CreateCreativeContentPacket();
    internal static readonly byte[] SerializedCreativeContent = SerializeCreativeContent(CreativeContentPacket);

    public static ItemType? Find(string identifier) => ByIdentifier.GetValueOrDefault(identifier);

    public static ItemType? FindByNetworkId(int networkId) => ByNetworkId.GetValueOrDefault(networkId);

    static ItemRegistryPacket CreateRegistryPacket() {
        ItemData[] items = new ItemData[Types.Length];
        for (int i = 0; i < Types.Length; i++) {
            ItemType type = Types[i];
            items[i] = new ItemData {
                Name = type.Identifier,
                Id = checked((short)type.NetworkId),
                ComponentBased = type.ComponentBased,
                Version = type.Version,
                ComponentData = type.ComponentBased
                    ? type.PropertiesPayload.ToComponentData()
                    : new CompoundTag()
            };
        }

        return new ItemRegistryPacket { Items = items };
    }

    static byte[] SerializeRegistry(ItemRegistryPacket packet) {
        using BinaryStream stream = BinaryStream.Rent(2 * 1024 * 1024);
        BinaryWriter writer = stream.GetWriter();
        packet.Serialize(ref writer);
        return stream.Buffer.Span[..stream.Offset].ToArray();
    }

    static CreativeContentPacket CreateCreativeContentPacket() {
        Dictionary<(CreativeItemCategory Category, string Name), int> groupIndices = [];
        List<CreativeGroupInfoPayload> groups = [];
        List<CreativeItemEntryPayload> entries = [];

        foreach (ItemType type in Types) {
            if (type.Catalog is null || type.NetworkId == 0)
                continue;

            CreativeItemCategory category = type.Catalog.CategoryName switch {
                "construction" => CreativeItemCategory.Construction,
                "nature" => CreativeItemCategory.Nature,
                "equipment" => CreativeItemCategory.Equipment,
                "items" => CreativeItemCategory.Items,
                _ => throw new InvalidDataException($"Unknown creative item category '{type.Catalog.CategoryName}'.")
            };

            string groupName = type.Catalog.GroupIdentifier?.Name ?? string.Empty;
            string groupIcon = type.Catalog.GroupIdentifier?.Icon ?? string.Empty;
            (CreativeItemCategory Category, string Name) groupKey = (category, groupName);
            if (!groupIndices.TryGetValue(groupKey, out int groupIndex)) {
                ItemType iconType = Find(groupIcon) ?? Find("minecraft:air")!;
                int iconBlockRuntimeId = BlockPalette.FindType(iconType.Identifier) is null
                    ? 0
                    : BlockPalette.FindDefaultPermutation(iconType.Identifier)?.NetworkId ?? 0;

                groupIndex = groups.Count;
                groupIndices.Add(groupKey, groupIndex);
                groups.Add(new CreativeGroupInfoPayload {
                    CreativeCategory = category,
                    Name = groupName,
                    GroupIconItem = new NetworkItemInstanceDescriptor {
                        Id = iconType.NetworkId,
                        StackSize = 1,
                        BlockRuntimeId = iconBlockRuntimeId
                    }
                });
            }

            int blockRuntimeId = BlockPalette.FindType(type.Identifier) is null
                ? 0
                : BlockPalette.FindDefaultPermutation(type.Identifier)?.NetworkId ?? 0;
            entries.Add(new CreativeItemEntryPayload {
                CreativeNetId = entries.Count + 1,
                ItemInstance = new NetworkItemInstanceDescriptor {
                    Id = type.NetworkId,
                    StackSize = 1,
                    BlockRuntimeId = blockRuntimeId
                },
                GroupIndex = checked((uint)groupIndex)
            });
        }

        return new CreativeContentPacket {
            Groups = groups.ToArray(),
            Entries = entries.ToArray()
        };
    }

    static byte[] SerializeCreativeContent(CreativeContentPacket packet) {
        using BinaryStream stream = BinaryStream.Rent(2 * 1024 * 1024);
        BinaryWriter writer = stream.GetWriter();
        packet.Serialize(ref writer);
        return stream.Buffer.Span[..stream.Offset].ToArray();
    }

    static ItemType[] LoadTypes() {
        Assembly dataAssembly = Assembly.Load("BasaltBE.Data");
        using Stream stream = dataAssembly.GetManifestResourceStream("BasaltBE.Data.Data.item-types.json")
            ?? throw new InvalidOperationException("Item data file 'item-types.json' is missing.");

        ItemType[] types = ItemTypeParser.Parse(stream);
        foreach (ItemType type in types) {
            string identifier = type.Identifier;
            if (type.PropertiesPayload.Values.GetValueOrDefault("minecraft:block_placer") is CompoundTag placer &&
                placer.Get<StringTag>("block") is { } block)
                identifier = block.Value;

            type.PlacementPermutation = BlockPalette.FindDefaultPermutation(identifier);
        }
        return types;
    }
}
