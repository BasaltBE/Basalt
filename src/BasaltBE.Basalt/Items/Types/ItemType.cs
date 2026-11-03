using System.Text.Json.Serialization;
using BasaltBE.Protocol.Enums;
using BasaltBE.Basalt.Blocks;

namespace BasaltBE.Basalt.Items.Types;

public sealed class ItemType {
    [JsonIgnore]
    internal BlockPermutation? PlacementPermutation;

    [JsonIgnore]
    internal BlockPermutation? SeedCropPermutation;

    [JsonIgnore]
    internal ToolType ToolType;

    [JsonIgnore]
    internal int ToolTier;

    [JsonIgnore]
    internal ToolMaterial ToolMaterial;

    [JsonIgnore]
    internal float MiningSpeed = 1;

    [JsonIgnore]
    internal float AttackDamageBonus;

    [JsonIgnore]
    internal int AttackDurabilityDamage;

    [JsonIgnore]
    internal int BlockDurabilityDamage;

    [JsonIgnore]
    internal byte DurabilityDamageChanceMinimum = 100;

    [JsonIgnore]
    internal byte DurabilityDamageChanceMaximum = 100;

    [JsonInclude, JsonPropertyName("identifier")]
    public string Identifier = string.Empty;

    [JsonInclude, JsonPropertyName("blockIdentifier")]
    public string? BlockIdentifier;

    [JsonInclude, JsonPropertyName("catalog")]
    public ItemCatalog? Catalog;

    [JsonInclude, JsonPropertyName("tags")]
    public string[] Tags = [];

    [JsonInclude, JsonPropertyName("stackable")]
    public bool Stackable;

    [JsonInclude, JsonPropertyName("maxAmount")]
    public int MaxAmount;

    [JsonInclude, JsonPropertyName("componentBased")]
    public bool ComponentBased;

    [JsonInclude, JsonPropertyName("networkId")]
    public int NetworkId;

    [JsonInclude, JsonPropertyName("itemVersion")]
    public ItemVersion Version;

    [JsonInclude, JsonPropertyName("legacyName")]
    public string? LegacyName;

    [JsonInclude, JsonPropertyName("propertiesPayload")]
    public ItemPropertiesPayload PropertiesPayload = new();

    [JsonIgnore]
    public int MaxDurability;

    [JsonIgnore]
    public int Enchantability;

    [JsonIgnore]
    public ArmorSlot? ArmorSlot;

    [JsonIgnore]
    public int ArmorProtection;

    [JsonIgnore]
    public int FoodNutrition;

    [JsonIgnore]
    public float FoodSaturationModifier;

    [JsonIgnore]
    public bool FoodCanAlwaysEat;

    [JsonIgnore]
    public int FoodUseDurationTicks = 32;
}
