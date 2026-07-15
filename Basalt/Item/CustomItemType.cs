namespace Basalt.Core.Item;

using Basalt.Protocol.Nbt;


/// <summary>
/// Options for defining a custom item type.
/// </summary>
public sealed class CustomItemTypeOptions
{
  /// <summary>
  /// The namespaced identifier (e.g. "mynamespace:ruby").
  /// </summary>
  public required string Identifier { get; init; }

  /// <summary>
  /// Maximum stack size. Defaults to 64.
  /// </summary>
  public int MaxStackSize { get; init; } = 64;

  /// <summary>
  /// Display name shown in the UI.
  /// </summary>
  public string? DisplayName { get; init; }

  /// <summary>
  /// Texture name for the item icon (matches a resource pack texture key).
  /// </summary>
  public string? Icon { get; init; }

  /// <summary>
  /// Tags to associate with this item (e.g. "minecraft:is_sword").
  /// </summary>
  public IReadOnlyList<string>? Tags { get; init; }

  /// <summary>
  /// Whether the item renders held like a tool.
  /// </summary>
  public bool HandEquipped { get; init; }

  /// <summary>
  /// Maximum durability. Zero means no durability component.
  /// </summary>
  public int MaxDurability { get; init; }

  /// <summary>
  /// Creative inventory category name (construction, nature, equipment, items).
  /// Null means the item won't appear in creative.
  /// </summary>
  public string? CreativeCategory { get; init; }

  /// <summary>
  /// Creative inventory group name.
  /// </summary>
  public string? CreativeGroup { get; init; }

  /// <summary>
  /// Creative inventory group icon identifier.
  /// </summary>
  public string? CreativeGroupIcon { get; init; }

  /// <summary>
  /// Whether this item can destroy blocks in creative mode.
  /// </summary>
  public bool CanDestroyInCreative { get; init; } = true;

  /// <summary>
  /// Digger component defining block destruction speeds.
  /// Null means no digger behavior.
  /// </summary>
  public CustomItemDiggerOptions? Digger { get; init; }

  /// <summary>
  /// Allow burn time (seconds) for use as fuel. Zero means not fuel.
  /// </summary>
  public float FuelDuration { get; init; }

  /// <summary>
  /// Food component options. Null means not edible.
  /// </summary>
  public CustomItemFoodOptions? Food { get; init; }
}

/// <summary>
/// Options for the food component of a custom item.
/// </summary>
public sealed class CustomItemFoodOptions
{
  public int Nutrition { get; init; }
  public float SaturationModifier { get; init; }
  public bool CanAlwaysEat { get; init; }
  public string? UsingConvertsTo { get; init; }
}

/// <summary>
/// Options for the digger component of a custom item.
/// </summary>
public sealed class CustomItemDiggerOptions
{
  /// <summary>
  /// Speed at which targeted blocks are destroyed.
  /// </summary>
  public required float DestroySpeed { get; init; }

  /// <summary>
  /// Block tags this tool can efficiently mine (e.g. "minecraft:is_pickaxe_item_destructible").
  /// </summary>
  public required IReadOnlyList<string> BlockTags { get; init; }
}

/// <summary>
/// Factory for creating and registering custom item types.
/// Network IDs start at 20000 to avoid conflicts with vanilla items.
/// </summary>
public static class CustomItemType
{
  private static int _nextNetworkId = 20000;

  /// <summary>
  /// Creates and registers a new custom item type.
  /// </summary>
  public static ItemType Create(CustomItemTypeOptions options)
  {
    int networkId = ++_nextNetworkId;
    CompoundTag properties = BuildProperties(options, networkId);
    ItemCatalog? catalog = BuildCatalog(options);

    var type = new ItemType(
      options.Identifier,
      networkId,
      options.MaxStackSize,
      options.Tags,
      isComponentBased: true,
      version: 1,
      properties,
      catalog);

    ItemPalette.InvalidateCache();
    return type;
  }

  private static CompoundTag BuildProperties(CustomItemTypeOptions options, int networkId)
  {
    CompoundTag properties = new();
    properties.Set("id", new IntTag { Value = networkId });
    properties.Set("name", new StringTag { Value = options.Identifier });

    CompoundTag components = new();
    CompoundTag itemProperties = new();

    // Icon.
    if (!string.IsNullOrEmpty(options.Icon))
    {
      CompoundTag icon = new();
      CompoundTag textures = new();
      textures.Set("default", new StringTag { Value = options.Icon });
      icon.Set("textures", textures);
      itemProperties.Set("minecraft:icon", icon);
    }

    // Max stack size.
    itemProperties.Set("max_stack_size", new IntTag { Value = options.MaxStackSize });

    // Hand equipped.
    if (options.HandEquipped)
    {
      itemProperties.Set("hand_equipped", new ByteTag { Value = 1 });
    }

    if (itemProperties.Values.Count > 0)
    {
      components.Set("item_properties", itemProperties);
    }

    // Display name.
    if (!string.IsNullOrEmpty(options.DisplayName))
    {
      CompoundTag displayName = new();
      displayName.Set("value", new StringTag { Value = options.DisplayName });
      components.Set("minecraft:display_name", displayName);
    }

    // Durability.
    if (options.MaxDurability > 0)
    {
      CompoundTag durability = new();
      durability.Set("max_durability", new IntTag { Value = options.MaxDurability });
      CompoundTag damageChance = new();
      damageChance.Set("min", new IntTag { Value = 100 });
      damageChance.Set("max", new IntTag { Value = 100 });
      durability.Set("damage_chance", damageChance);
      components.Set("minecraft:durability", durability);
    }

    // Food.
    if (options.Food is not null)
    {
      CompoundTag food = new();
      food.Set("nutrition", new IntTag { Value = options.Food.Nutrition });
      food.Set("saturation_modifier", new FloatTag { Value = options.Food.SaturationModifier });
      food.Set("can_always_eat", new ByteTag { Value = options.Food.CanAlwaysEat ? (sbyte)1 : (sbyte)0 });
      food.Set("using_converts_to", new StringTag { Value = options.Food.UsingConvertsTo ?? string.Empty });
      components.Set("minecraft:food", food);
    }

    // Digger.
    if (options.Digger is not null)
    {
      CompoundTag digger = new();
      ListTag destroySpeeds = new();

      foreach (string blockTag in options.Digger.BlockTags)
      {
        CompoundTag entry = new();
        CompoundTag block = new();
        block.Set("tags", new StringTag { Value = $"query.any_tag('{blockTag}')" });
        entry.Set("block", block);
        entry.Set("speed", new IntTag { Value = (int)options.Digger.DestroySpeed });
        destroySpeeds.Values.Add(entry);
      }

      digger.Set("destroy_speeds", destroySpeeds);
      digger.Set("use_efficiency", new ByteTag { Value = 1 });
      components.Set("minecraft:digger", digger);
    }

    // Can destroy in creative.
    if (!options.CanDestroyInCreative)
    {
      CompoundTag canDestroy = new();
      canDestroy.Set("value", new ByteTag { Value = 0 });
      components.Set("minecraft:can_destroy_in_creative", canDestroy);
    }

    properties.Set("components", components);
    return properties;
  }

  private static ItemCatalog? BuildCatalog(CustomItemTypeOptions options)
  {
    if (string.IsNullOrEmpty(options.CreativeCategory))
    {
      return null;
    }

    return new ItemCatalog(options.CreativeCategory, options.CreativeGroup, options.CreativeGroupIcon);
  }
}
