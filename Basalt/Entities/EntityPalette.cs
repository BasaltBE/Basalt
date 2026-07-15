namespace Basalt.Core.Entities;

using System.Runtime.CompilerServices;
using System.Text.Json;
using Basalt.Core.Entities.Traits;
using Basalt.Protocol.Nbt;

public sealed class EntityPalette
{
    private const string PlayerIdentifier = "minecraft:player";
    private static bool _vanillaLoaded;
    private static readonly object LoadLock = new();

#pragma warning disable CA2255
    [ModuleInitializer]
    public static void Initialize()
#pragma warning restore CA2255
    {
#pragma warning disable IL2026
        LoadVanilla();
        EntityTraitRegistry.RegisterFromAssembly(typeof(EntityTraitRegistry).Assembly);
#pragma warning restore IL2026
    }

    public static IReadOnlyDictionary<string, EntityType> TypesMap => EntityType.Types;

    public static List<EntityType> GetAllTypes()
    {
        return EntityType.GetAll();
    }

    public static EntityType ResolveType(string identifier)
    {
        return EntityType.GetOrCreate(identifier);
    }

    public static void RegisterTrait<TTrait>() where TTrait : EntityTrait
    {
        EntityTraitRegistry.Register<TTrait>();
    }

    public static void RegisterTrait(params Type[] traitTypes)
    {
        EntityTraitRegistry.Register(traitTypes);
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

                _ = new EntityType(entry.Identifier, entry.Components, entry.PropertiesPayload, entry.Loot?.Table);
            }

            if (EntityType.Get(PlayerIdentifier) is null)
            {
                _ = new EntityType(PlayerIdentifier, []);
            }

            global::Basalt.Core.Loot.LootTableManager.LoadFromEntities(root, EntityType.GetAll());
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






