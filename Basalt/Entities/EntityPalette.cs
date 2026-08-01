namespace Basalt.Core.Entities;

using System.Runtime.CompilerServices;
using System.Text.Json;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Loot;
using Basalt.Protocol.Nbt;

public sealed class EntityPalette {
    private const string PlayerIdentifier = "minecraft:player";
    private const string RideableComponent = "minecraft:rideable";
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

    public static List<EntityType> GetAllTypes() {
        return EntityType.GetAll();
    }

    public static EntityType ResolveType(string identifier) {
        return EntityType.GetOrCreate(identifier);
    }

    public static void RegisterTrait<TTrait>() where TTrait : EntityTrait {
        EntityTraitRegistry.Register<TTrait>();
    }

    public static void RegisterTrait(params Type[] traitTypes) {
        EntityTraitRegistry.Register(traitTypes);
    }

    private static byte[]? _actorIdentifiersPayload;

    public static CompoundTag BuildAvailableActorIdentifiersTag() {
        LoadVanilla();

        CompoundTag root = new();
        ListTag idList = new();

        foreach (EntityType type in EntityType.GetAll()) {
            CompoundTag entry = new();
            entry.Set("bid", new StringTag { Value = string.Empty });
            entry.Set("id", new StringTag { Value = type.Identifier });
            idList.Values.Add(entry);
        }

        root.Set("idlist", idList);
        return root;
    }

    public static byte[] GetActorIdentifiersPayload() {
        if (_actorIdentifiersPayload is not null) {
            return _actorIdentifiersPayload;
        }

        CompoundTag data = BuildAvailableActorIdentifiersTag();
        using Basalt.Binary.BinaryStream stream = Basalt.Binary.BinaryStream.Rent(64 * 1024);
        Basalt.Binary.BinaryWriter writer = stream;

        Protocol.Packets.AvailableActorIdentifiersPacket packet = new() { Data = data };
        packet.Serialize(writer);

        _actorIdentifiersPayload = writer.GetProcessedBytes().ToArray();
        return _actorIdentifiersPayload;
    }

    public static void LoadVanilla(string? dataDirectory = null) {
        if (_vanillaLoaded) {
            return;
        }

        lock (LoadLock) {
            if (_vanillaLoaded) {
                return;
            }

            List<EntityTypeData> types;
            if (!string.IsNullOrWhiteSpace(dataDirectory)) {
                string typesPath = Path.Combine(dataDirectory, "entity_types.json");
                using FileStream fileStream = File.OpenRead(typesPath);
                types = JsonSerializer.Deserialize(fileStream, EntityPaletteJsonContext.Default.ListEntityTypeData) ?? [];
            }
            else {
                using Stream stream = ProtocolData.Require("entity_types.json");
                types = JsonSerializer.Deserialize(stream, EntityPaletteJsonContext.Default.ListEntityTypeData) ?? [];
            }

            EntityType.EnsureRegistryCapacity(types.Count + 1);

            for (int i = 0; i < types.Count; i++) {
                EntityTypeData entry = types[i];
                if (string.IsNullOrEmpty(entry.Identifier) || EntityType.Get(entry.Identifier) is not null) {
                    continue;
                }

                if (entry.Identifier == PlayerIdentifier) {
                    entry.Components.Remove(RideableComponent);
                    entry.PropertiesPayload?.Components.Remove(RideableComponent);
                }

                _ = new EntityType(entry.Identifier, entry.Components, entry.PropertiesPayload, entry.Loot?.Table);
            }

            if (EntityType.Get(PlayerIdentifier) is null) {
                _ = new EntityType(PlayerIdentifier, []);
            }

            if (!string.IsNullOrWhiteSpace(dataDirectory)) {
                LootTableManager.LoadFromEntities(dataDirectory, EntityType.GetAll());
            }
            else {
                using Stream stream = ProtocolData.Require("entity_drops.json");
                LootTableManager.LoadFromEntities(stream, EntityType.GetAll());
            }

            _vanillaLoaded = true;
        }
    }

}






