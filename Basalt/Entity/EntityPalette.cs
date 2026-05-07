using System.Runtime.CompilerServices;
using System.Text.Json;
using Basalt.Protocol.Nbt;

namespace Basalt.Entity;

public sealed class EntityPalette
{
    private const string PlayerIdentifier = "minecraft:player";
    private static bool _vanillaLoaded;
    private static readonly object LoadLock = new();

    [ModuleInitializer]
    public static void Initialize()
    {
        LoadVanilla();
    }

    public IReadOnlyDictionary<string, EntityType> Types => EntityType.Types;

    public List<EntityType> GetAllTypes()
    {
        return EntityType.GetAll();
    }

    public EntityType ResolveType(string identifier)
    {
        return EntityType.GetOrPlayer(identifier);
    }

    public static CompoundTag BuildAvailableActorIdentifiersTag()
    {
        LoadVanilla();

        CompoundTag root = new();
        ListTag idList = new();

        foreach (EntityType type in EntityType.GetAll())
        {
            CompoundTag entry = new();
            entry.Set("identifier", new StringTag { Value = type.Identifier });

            ListTag components = new();
            for (int i = 0; i < type.Components.Count; i++)
            {
                components.Values.Add(new StringTag { Value = type.Components[i] });
            }

            entry.Set("components", components);
            idList.Values.Add(entry);
        }

        root.Set("idlist", idList);
        return root;
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

            string root = ResolveDataDirectory(dataDirectory);
            string typesPath = Path.Combine(root, "entity_types.json");
            using FileStream stream = File.OpenRead(typesPath);
            List<EntityTypeData> types = JsonSerializer.Deserialize(stream, EntityPaletteJsonContext.Default.ListEntityTypeData) ?? [];

            EntityType.EnsureRegistryCapacity(types.Count + 1);

            for (int i = 0; i < types.Count; i++)
            {
                EntityTypeData entry = types[i];
                if (string.IsNullOrEmpty(entry.Identifier) || EntityType.Get(entry.Identifier) is not null)
                {
                    continue;
                }

                _ = new EntityType(entry.Identifier, entry.Components);
            }

            _ = EntityType.Get(PlayerIdentifier) ?? new EntityType(PlayerIdentifier, []);
            _vanillaLoaded = true;
        }
    }

    private static string ResolveDataDirectory(string? overrideDirectory)
    {
        if (!string.IsNullOrWhiteSpace(overrideDirectory))
        {
            return overrideDirectory;
        }

        string? current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            string candidate = Path.Combine(current, "Protocol", "Data");
            if (Directory.Exists(candidate))
            {
                return candidate;
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
}
