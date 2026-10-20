using System.Reflection;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Items;

public static class ItemPalette {
    public static readonly ItemType[] Types = LoadTypes();
    static readonly Dictionary<string, ItemType> ByIdentifier = Types.ToDictionary(type => type.Identifier, StringComparer.Ordinal);
    static readonly Dictionary<int, ItemType> ByNetworkId = Types.ToDictionary(type => type.NetworkId);
    internal static readonly ItemRegistryPacket RegistryPacket = CreateRegistryPacket();
    internal static readonly CreativeContentPacket CreativeContentPacket = CreateCreativeContentPacket();

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

    static ItemType[] LoadTypes() {
        Assembly dataAssembly = Assembly.Load("BasaltBE.Data");
        using Stream stream = dataAssembly.GetManifestResourceStream("BasaltBE.Data.Data.item-types.json")
            ?? throw new InvalidOperationException("Item data file 'item-types.json' is missing.");

        ItemType[] types = ItemTypeParser.Parse(stream);
        foreach (ItemType type in types) {
            foreach (string tag in type.Tags) {
                type.ToolType |= tag switch {
                    "minecraft:is_pickaxe" => ToolType.Pickaxe,
                    "minecraft:is_axe" => ToolType.Axe,
                    "minecraft:is_shovel" => ToolType.Shovel,
                    "minecraft:is_hoe" => ToolType.Hoe,
                    "minecraft:is_sword" => ToolType.Sword,
                    _ => ToolType.None
                };

                type.ToolMaterial = tag switch {
                    "minecraft:wooden_tier" => ToolMaterial.Wooden,
                    "minecraft:stone_tier" => ToolMaterial.Stone,
                    "minecraft:copper_tier" => ToolMaterial.Copper,
                    "minecraft:iron_tier" => ToolMaterial.Iron,
                    "minecraft:golden_tier" => ToolMaterial.Gold,
                    "minecraft:diamond_tier" => ToolMaterial.Diamond,
                    "minecraft:netherite_tier" => ToolMaterial.Netherite,
                    _ => type.ToolMaterial
                };
            }

            type.ToolTier = type.ToolMaterial switch {
                ToolMaterial.Stone or ToolMaterial.Copper => 1,
                ToolMaterial.Iron => 2,
                ToolMaterial.Diamond or ToolMaterial.Netherite => 3,
                _ => 0
            };
            type.MiningSpeed = type.ToolType == ToolType.None ? 1 : type.ToolMaterial switch {
                ToolMaterial.Wooden => 2,
                ToolMaterial.Stone => 4,
                ToolMaterial.Copper => 5,
                ToolMaterial.Iron => 6,
                ToolMaterial.Gold => 12,
                ToolMaterial.Diamond => 8,
                ToolMaterial.Netherite => 9,
                _ => 1
            };
            type.AttackDamageBonus = (type.ToolType, type.ToolMaterial) switch {
                (ToolType.Sword, ToolMaterial.Wooden or ToolMaterial.Gold) => 3,
                (ToolType.Sword, ToolMaterial.Stone or ToolMaterial.Copper) => 4,
                (ToolType.Sword, ToolMaterial.Iron) => 5,
                (ToolType.Sword, ToolMaterial.Diamond) => 6,
                (ToolType.Sword, ToolMaterial.Netherite) => 7,
                (ToolType.Axe, ToolMaterial.Wooden or ToolMaterial.Gold) => 6,
                (ToolType.Axe, ToolMaterial.Stone or ToolMaterial.Copper or ToolMaterial.Iron or ToolMaterial.Diamond) => 8,
                (ToolType.Axe, ToolMaterial.Netherite) => 9,
                _ => 0
            };

            string identifier = type.Identifier == "minecraft:fence"
                ? "minecraft:oak_fence"
                : type.Identifier;
            if (type.PropertiesPayload.Values.GetValueOrDefault("minecraft:block_placer") is CompoundTag placer &&
                placer.Get<StringTag>("block") is { } block)
                identifier = block.Value;

            type.PlacementPermutation = BlockPalette.FindDefaultPermutation(identifier);
        }
        return types;
    }
}
