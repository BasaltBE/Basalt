namespace Basalt.Core.Item;

using Basalt.Core.Blocks;

public sealed class CustomItemTypeBuilder {
    private readonly CustomItemTypeOptions _options = new() {
        Identifier = string.Empty
    };

    public CustomItemTypeBuilder SetIdentifier(string identifier) {
        _options.Identifier = identifier;
        return this;
    }

    public CustomItemTypeBuilder SetMaxStackSize(int maxStackSize) {
        _options.MaxStackSize = maxStackSize;
        return this;
    }

    public CustomItemTypeBuilder SetDisplayName(string displayName) {
        _options.DisplayName = displayName;
        return this;
    }

    public CustomItemTypeBuilder SetIcon(string icon) {
        _options.Icon = icon;
        return this;
    }

    public CustomItemTypeBuilder SetTags(IReadOnlyList<string> tags) {
        _options.Tags = tags;
        return this;
    }

    public CustomItemTypeBuilder SetHandEquipped(bool handEquipped = true) {
        _options.HandEquipped = handEquipped;
        return this;
    }

    public CustomItemTypeBuilder SetMaxDurability(int maxDurability) {
        _options.MaxDurability = maxDurability;
        return this;
    }

    public CustomItemTypeBuilder SetCreativeCategory(CreativeCategory category) {
        _options.CreativeCategory = category switch {
            CreativeCategory.Construction => "construction",
            CreativeCategory.Nature => "nature",
            CreativeCategory.Equipment => "equipment",
            CreativeCategory.Items => "items",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
        return this;
    }

    public CustomItemTypeBuilder SetCreativeCategory(string category) {
        _options.CreativeCategory = category;
        return this;
    }

    public CustomItemTypeBuilder SetCreativeGroup(string creativeGroup) {
        _options.CreativeGroup = creativeGroup;
        return this;
    }

    public CustomItemTypeBuilder SetCreativeGroupIcon(string creativeGroupIcon) {
        _options.CreativeGroupIcon = creativeGroupIcon;
        return this;
    }

    public CustomItemTypeBuilder SetBlockType(BlockType blockType) {
        _options.BlockType = blockType;
        return this;
    }

    public CustomItemTypeBuilder SetCanDestroyInCreative(bool canDestroyInCreative) {
        _options.CanDestroyInCreative = canDestroyInCreative;
        return this;
    }

    public CustomItemTypeBuilder SetDigger(CustomItemDiggerOptions digger) {
        _options.Digger = digger;
        return this;
    }

    public CustomItemTypeBuilder SetFuelDuration(float fuelDuration) {
        _options.FuelDuration = fuelDuration;
        return this;
    }

    public CustomItemTypeBuilder SetFood(CustomItemFoodOptions food) {
        _options.Food = food;
        return this;
    }

    public CustomItemTypeBuilder SetAttackDamage(float attackDamage) {
        _options.AttackDamage = attackDamage;
        return this;
    }

    public CustomItemTypeBuilder SetWearable(CustomItemWearableOptions wearable) {
        _options.Wearable = wearable;
        return this;
    }

    public ItemType Build() {
        if (string.IsNullOrWhiteSpace(_options.Identifier)) {
            throw new InvalidOperationException("A custom item identifier is required.");
        }

        return CustomItemType.Create(_options);
    }
}
