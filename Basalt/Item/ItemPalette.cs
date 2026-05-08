using Basalt.Binary;
using Basalt.Item.Traits;
using Basalt.Protocol.Types;
using Basalt.Protocol.Nbt;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Text.Json;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Item;

public sealed class ItemPalette
{
    private const string AirIdentifier = "minecraft:air";
    private static bool _vanillaLoaded;
    private static readonly object LoadLock = new();
    private static byte[]? _itemRegistryPayload;

    [ModuleInitializer]
    public static void Initialize()
    {
        LoadVanilla();
        ItemTraitRegistry.RegisterFromAssembly(Assembly.GetExecutingAssembly());
    }

    public IReadOnlyDictionary<string, ItemType> Types => ItemType.Types;

    public List<ItemType> GetAllTypes()
    {
        return ItemType.GetAll();
    }

    public static byte[] GetItemRegistryPayload()
    {
        if (_itemRegistryPayload is not null)
        {
            return _itemRegistryPayload;
        }

        lock (LoadLock)
        {
            if (_itemRegistryPayload is not null)
            {
                return _itemRegistryPayload;
            }

            LoadVanilla();

            List<ItemEntry> items = [.. ItemType.GetAll().Select(type => new ItemEntry
            {
                Name = type.Identifier,
                RuntimeId = checked((short)type.NetworkId),
                ComponentBased = type.IsComponentBased,
                Version = type.Version,
                Data = type.Properties
            })];

            _itemRegistryPayload = SerializeItemRegistryBody(items);
            return _itemRegistryPayload;
        }
    }

    public ItemType ResolveType(string identifier)
    {
        return ItemType.GetOrAir(identifier);
    }

    public ItemType ResolveType(int networkId)
    {
        return ItemType.GetByNetwork(networkId) ?? ItemType.GetOrAir(AirIdentifier);
    }

    public static void LoadVanilla(string? dataDirectory = null)
    {
        if (_vanillaLoaded)
        {
            return;
        }

        lock (LoadLock)
        {
            if (_vanillaLoaded)
            {
                return;
            }

            string root;
            if (!string.IsNullOrWhiteSpace(dataDirectory))
            {
                root = dataDirectory;
            }
            else
            {
                string? current = AppContext.BaseDirectory;
                while (!string.IsNullOrEmpty(current))
                {
                    string candidate = Path.Combine(current, "Protocol", "Data");
                    if (Directory.Exists(candidate))
                    {
                        root = candidate;
                        goto RootResolved;
                    }

                    DirectoryInfo? parent = Directory.GetParent(current);
                    if (parent is null)
                    {
                        break;
                    }

                    current = parent.FullName;
                }

                throw new DirectoryNotFoundException("Could not locate Protocol/Data directory.");
            }

        RootResolved:
            string typesPath = Path.Combine(root, "item_types.json");
            string metadataPath = Path.Combine(root, "item_metadata.json");
            List<ItemTypeData> types;
            using (FileStream typesStream = File.OpenRead(typesPath))
            {
                types = JsonSerializer.Deserialize(typesStream, ItemPaletteJsonContext.Default.ListItemTypeData) ?? [];
            }

            List<ItemMetadataData> metadata;
            using (FileStream metadataStream = File.OpenRead(metadataPath))
            {
                metadata = JsonSerializer.Deserialize(metadataStream, ItemPaletteJsonContext.Default.ListItemMetadataData) ?? [];
            }

            ItemType.EnsureRegistryCapacity(types.Count + 1);
            Dictionary<string, ItemTypeData> typeMap = new(StringComparer.Ordinal);
            for (int i = 0; i < types.Count; i++)
            {
                ItemTypeData type = types[i];
                if (string.IsNullOrEmpty(type.Identifier))
                {
                    continue;
                }

                typeMap[type.Identifier] = type;
            }

            for (int i = 0; i < metadata.Count; i++)
            {
                ItemMetadataData entry = metadata[i];
                if (string.IsNullOrEmpty(entry.Identifier) || ItemType.Get(entry.Identifier) is not null)
                {
                    continue;
                }

                if (!typeMap.TryGetValue(entry.Identifier, out ItemTypeData? typeData))
                {
                    continue;
                }

                CompoundTag properties;
                if (string.IsNullOrWhiteSpace(entry.Properties))
                {
                    properties = new CompoundTag();
                }
                else
                {
                    byte[] data = Convert.FromBase64String(entry.Properties);
                    if (data.Length == 0)
                    {
                        properties = new CompoundTag();
                    }
                    else
                    {
                        BinaryReader reader = new(data);
                        TagType rootType = (TagType)reader.ReadInt8();
                        if (rootType != TagType.Compound)
                        {
                            throw new InvalidOperationException($"Unexpected item properties root tag type '{rootType}'.");
                        }

                        properties = CompoundTag.Read(ref reader);
                    }
                }

                _ = new ItemType(
                    entry.Identifier,
                    entry.NetworkId,
                    typeData.MaxAmount,
                    typeData.Tags,
                    entry.IsComponentBased,
                    entry.ItemVersion,
                    properties);
            }

            _ = ItemType.Get(AirIdentifier) ?? new ItemType(AirIdentifier, 0, 64, [], true, 1);
            _vanillaLoaded = true;
        }
    }

    private static byte[] SerializeItemRegistryBody(List<ItemEntry> items)
    {
        int size = Math.Max(1024, (items.Count * 256) + 8);
        while (true)
        {
            byte[] buffer = new byte[size];
            try
            {
                BinaryWriter writer = new(buffer);
                writer.WriteVarUInt((uint)items.Count);
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].Write(ref writer);
                }

                return writer.GetBuffer().ToArray();
            }
            catch (ArgumentOutOfRangeException)
            {
                size *= 2;
            }
        }
    }
}
