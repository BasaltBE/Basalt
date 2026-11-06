using Basalt.Binary;
using System.Collections.Frozen;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Nbt.Enums;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.Items;

public sealed class CustomItem {
    public readonly ItemType Type;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly CompoundTag properties;
    private string[]? diggerTags;

    internal CustomItem(ItemType type) {
        Type = type;
        properties = Type.PropertiesPayload.Values.GetValueOrDefault("item_properties") as CompoundTag ?? new();
        Type.PropertiesPayload.Values["item_properties"] = properties;
        Type.PropertiesPayload.MaxAmount = Type.MaxAmount;
        Type.PropertiesPayload.Stackable = Type.Stackable;
        Type.PropertiesPayload.ItemTags = Type.Tags;

        if (properties.Get<IntTag>("max_stack_size") is null)
            properties.Add("max_stack_size", new IntTag { Value = Type.MaxAmount });

        if (properties.Get<ByteTag>("can_destroy_in_creative") is null)
            properties.Add("can_destroy_in_creative", new ByteTag { Value = Type.CanDestroyInCreative ? (byte)1 : (byte)0 });
    }

    public CustomItem SetDisplayName(string name) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return SetComponent("minecraft:display_name", new CompoundTag().Add("value", new StringTag { Value = name }));
    }

    public CustomItem SetIcon(string texture) {
        ArgumentException.ThrowIfNullOrWhiteSpace(texture);
        return SetProperty("minecraft:icon", new CompoundTag().Add("textures",
            new CompoundTag().Add("default", new StringTag { Value = texture })));
    }

    public CustomItem SetHoverTextColor(string color) {
        ArgumentException.ThrowIfNullOrWhiteSpace(color);
        return SetComponent("minecraft:hover_text_color", new CompoundTag().Add("value", new StringTag { Value = color }));
    }

    public CustomItem SetMaxStackSize(int size) {
        if (size is < 1 or > 255)
            throw new ArgumentOutOfRangeException(nameof(size), "Stack size must be from 1 to 255.");
        Mutable();
        Type.MaxAmount = size;
        Type.Stackable = size > 1;
        Type.PropertiesPayload.MaxAmount = size;
        Type.PropertiesPayload.Stackable = size > 1;
        properties.Add("max_stack_size", new IntTag { Value = size });
        return this;
    }

    public CustomItem SetTags(params string[] tags) {
        ArgumentNullException.ThrowIfNull(tags);
        if (tags.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Item tags must be non-empty identifiers.", nameof(tags));
        Mutable();
        Type.Tags = tags.Distinct(StringComparer.Ordinal).ToArray();
        Type.PropertiesPayload.ItemTags = Type.Tags;
        ListTag list = new() { ElementType = TagType.String };
        foreach (string tag in Type.Tags)
            list.Value.Add(new StringTag { Value = tag });
        return SetComponent("minecraft:tags", new CompoundTag().Add("tags", list));
    }

    public CustomItem SetHandEquipped(bool value = true) => SetProperty("hand_equipped", new ByteTag { Value = value ? (byte)1 : (byte)0 });

    public CustomItem SetGlint(bool value = true) {
        SetProperty("foil", new ByteTag { Value = value ? (byte)1 : (byte)0 });
        return SetComponent("minecraft:foil", new CompoundTag().Add("value", new ByteTag { Value = value ? (byte)1 : (byte)0 }));
    }

    public CustomItem SetCanDestroyInCreative(bool value) {
        Mutable();
        Type.CanDestroyInCreative = value;
        return SetProperty("can_destroy_in_creative", new ByteTag { Value = value ? (byte)1 : (byte)0 });
    }

    public CustomItem SetDurability(int maximum, int damageChanceMinimum = 100, int damageChanceMaximum = 100) {
        if (maximum < 1 || damageChanceMinimum is < 0 or > 100 || damageChanceMaximum < damageChanceMinimum || damageChanceMaximum > 100)
            throw new ArgumentOutOfRangeException(nameof(maximum), "Use positive durability and an ordered damage chance from 0 to 100.");
        Mutable();
        SetMaxStackSize(1);
        Type.MaxDurability = maximum;
        Type.DurabilityDamageChanceMinimum = (byte)damageChanceMinimum;
        Type.DurabilityDamageChanceMaximum = (byte)damageChanceMaximum;
        return SetComponent("minecraft:durability", new CompoundTag()
            .Add("max_durability", new IntTag { Value = maximum })
            .Add("damage_chance", new CompoundTag()
                .Add("min", new IntTag { Value = damageChanceMinimum })
                .Add("max", new IntTag { Value = damageChanceMaximum })));
    }

    public CustomItem SetAttackDamage(float damage, int durabilityCost = 1) {
        if (!float.IsFinite(damage) || damage < 1 || damage > 32768 || durabilityCost < 0)
            throw new ArgumentOutOfRangeException(nameof(damage));
        Mutable();
        int bonus = (int)MathF.Round(damage) - 1;
        Type.AttackDamageBonus = bonus;
        Type.AttackDurabilityDamage = durabilityCost;
        properties.Add("damage", new IntTag { Value = bonus });
        return SetComponent("minecraft:damage", new CompoundTag().Add("value", new ShortTag { Value = (short)bonus }));
    }

    public CustomItem SetTool(ToolType tool, int tier, float miningSpeed, int blockDurabilityCost = 1) {
        if (tool == ToolType.None || (tool & ~(ToolType.Pickaxe | ToolType.Axe | ToolType.Shovel | ToolType.Hoe | ToolType.Sword)) != 0 ||
            tier < 0 || !float.IsFinite(miningSpeed) || miningSpeed <= 0 || blockDurabilityCost < 0)
            throw new ArgumentOutOfRangeException(nameof(tool));
        Mutable();

        Type.ToolType = tool;
        Type.ToolTier = tier;
        properties.Add("tier", new IntTag { Value = tier });
        Type.BlockDurabilityDamage = blockDurabilityCost;
        Type.AttackDurabilityDamage = tool == ToolType.Sword ? 1 : 2;
        List<string> tags = [.. Type.Tags.Where(tag => tag is not (
            "minecraft:is_pickaxe" or "minecraft:is_axe" or "minecraft:is_shovel" or "minecraft:is_hoe" or "minecraft:is_sword"))];

        if ((tool & ToolType.Pickaxe) != 0) tags.Add("minecraft:is_pickaxe");
        if ((tool & ToolType.Axe) != 0) tags.Add("minecraft:is_axe");
        if ((tool & ToolType.Shovel) != 0) tags.Add("minecraft:is_shovel");
        if ((tool & ToolType.Hoe) != 0) tags.Add("minecraft:is_hoe");
        if ((tool & ToolType.Sword) != 0) tags.Add("minecraft:is_sword");
        SetTags(tags.ToArray());

        List<string> blocks = [];
        if ((tool & ToolType.Pickaxe) != 0) blocks.Add("minecraft:is_pickaxe_item_destructible");
        if ((tool & ToolType.Axe) != 0) blocks.Add("minecraft:is_axe_item_destructible");
        if ((tool & ToolType.Shovel) != 0) blocks.Add("minecraft:is_shovel_item_destructible");
        if ((tool & ToolType.Hoe) != 0) blocks.Add("minecraft:is_hoe_item_destructible");
        return SetDigger(miningSpeed, blocks.ToArray());
    }

    public CustomItem SetDigger(float speed, params string[] blockTags) {
        if (!float.IsFinite(speed) || speed <= 0 || (double)speed > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(speed));
        ArgumentNullException.ThrowIfNull(blockTags);
        if (blockTags.Any(tag => string.IsNullOrWhiteSpace(tag) || tag.Any(character =>
            !char.IsAsciiLetterOrDigit(character) && character is not (':' or '_' or '-' or '.' or '/'))))
            throw new ArgumentException("Use block tag identifiers.", nameof(blockTags));
        Mutable();

        Type.MiningSpeed = Math.Max(1, (int)MathF.Round(speed));

        string[] tags = blockTags.Distinct(StringComparer.Ordinal).ToArray();
        diggerTags = tags;
        properties.Add("mining_speed", new FloatTag { Value = Type.MiningSpeed });

        return SetComponent("minecraft:digger", new CompoundTag()
            .Add("destroy_speeds", new ListTag { ElementType = TagType.Compound })
            .Add("use_efficiency", new ByteTag { Value = 1 }));
    }

    public CustomItem SetFood(int nutrition, float saturationModifier, bool canAlwaysEat = false,
        string? convertsTo = null, bool drink = false, int durationTicks = 32) {
        if (nutrition < 1 || !float.IsFinite(saturationModifier) || saturationModifier < 0 || durationTicks < 1)
            throw new ArgumentOutOfRangeException(nameof(nutrition));
        Mutable();

        Type.FoodNutrition = nutrition;
        Type.FoodSaturationModifier = saturationModifier;
        Type.FoodCanAlwaysEat = canAlwaysEat;
        Type.FoodConvertsTo = convertsTo;
        Type.FoodUseDurationTicks = durationTicks;

        properties.Add("use_duration", new IntTag { Value = durationTicks });
        properties.Add("use_animation", new IntTag { Value = drink ? 2 : 1 });

        SetComponent("minecraft:use_duration", new IntTag { Value = durationTicks });

        return SetComponent("minecraft:food", new CompoundTag()
            .Add("nutrition", new IntTag { Value = nutrition })
            .Add("saturation_modifier", new FloatTag { Value = saturationModifier })
            .Add("can_always_eat", new ByteTag { Value = canAlwaysEat ? (byte)1 : (byte)0 })
            .Add("using_converts_to", new StringTag { Value = convertsTo ?? string.Empty }));
    }

    public CustomItem SetFuel(float seconds) {
        if (!float.IsFinite(seconds) || seconds <= 0 || (double)seconds * 20 > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        Mutable();
        Type.FuelDurationTicks = Math.Max(1, (int)(seconds * 20));
        return SetComponent("minecraft:fuel", new CompoundTag().Add("duration", new FloatTag { Value = seconds }));
    }

    public CustomItem SetArmor(ArmorSlot slot, int protection, int toughness = 0) {
        if (!Enum.IsDefined(slot) || protection is < 0 or > 20 || toughness < 0)
            throw new ArgumentOutOfRangeException(nameof(slot));
        Mutable();
        SetMaxStackSize(1);
        Type.ArmorSlot = slot;
        Type.ArmorProtection = protection;
        Type.ArmorToughness = toughness;

        string[] slots = ["slot.armor.head", "slot.armor.chest", "slot.armor.legs", "slot.armor.feet"];
        SetTags([.. Type.Tags, "minecraft:is_armor"]);

        return SetComponent("minecraft:wearable", new CompoundTag()
            .Add("slot", new StringTag { Value = slots[(int)slot] })
            .Add("protection", new IntTag { Value = protection })
            .Add("toughness", new IntTag { Value = toughness }));
    }

    public CustomItem SetEnchantability(int value, string slot = "all") {
        if (value is < 0 or > byte.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value));
        ArgumentException.ThrowIfNullOrWhiteSpace(slot);
        Mutable();
        Type.Enchantability = value;
        properties.Add("enchantable_slot", new StringTag { Value = slot });
        properties.Add("enchantable_value", new IntTag { Value = value });

        return SetComponent("minecraft:enchantable", new CompoundTag()
            .Add("slot", new StringTag { Value = slot }).Add("value", new ByteTag { Value = (byte)value }));
    }

    public CustomItem SetPlacedBlock(string identifier) {
        BlockPermutation permutation = BlockPalette.FindDefaultPermutation(identifier)
            ?? throw new ArgumentException($"Unknown block '{identifier}'.", nameof(identifier));
        Mutable();
        Type.BlockIdentifier = identifier;
        Type.PlacementPermutation = permutation;

        return SetComponent("minecraft:block_placer", new CompoundTag()
            .Add("block", new StringTag { Value = identifier })
            .Add("canUseBlockAsIcon", new ByteTag { Value = 1 })
            .Add("use_on", new ListTag { ElementType = TagType.String }));
    }

    public CustomItem SetCreativeCategory(CreativeItemCategory category, string? group = null, string? groupIcon = null) {
        if (category is < CreativeItemCategory.Construction or > CreativeItemCategory.ItemCommandOnly)
            throw new ArgumentOutOfRangeException(nameof(category));
        Mutable();
        if (category == CreativeItemCategory.ItemCommandOnly)
        {
            Type.Catalog = null;
            return this;
        }

        string[] names = ["construction", "nature", "equipment", "items"];
        Type.Catalog = new ItemCatalog {
            CategoryName = names[(int)category - (int)CreativeItemCategory.Construction],
            GroupIdentifier = new ItemGroupIdentifier { Name = group ?? string.Empty, Icon = groupIcon ?? Type.Identifier }
        };
        return this;
    }

    public CustomItem SetComponent(string identifier, Tag data) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(data);
        if (data.Type == TagType.End)
            throw new ArgumentException("A component cannot be an End tag.", nameof(data));
        if (identifier is "item_properties" or "item_tags")
            throw new ArgumentException("Use item setters for properties and tags.", nameof(identifier));
        Mutable();

        using BinaryStream stream = BinaryStream.Rent(data.GetSize());
        BinaryWriter writer = stream.GetWriter();
        BasaltBE.Nbt.Nbt.Write(data, ref writer);
        int offset = 0;

        Tag copy = BasaltBE.Nbt.Nbt.Read(stream.Buffer.Span[..stream.Offset], ref offset);
        Type.PropertiesPayload.Values[identifier] = copy;
        if (!Type.PropertiesPayload.Components.Contains(identifier, StringComparer.Ordinal))
            Type.PropertiesPayload.Components = [.. Type.PropertiesPayload.Components, identifier];
        return this;
    }

    internal void Validate() {
        if (diggerTags is { } tags) {
            Type.DiggerBlocks = BlockPalette.Types.Where(block => tags.Any(tag => block.Tags.Contains(tag, StringComparer.Ordinal))).ToFrozenSet();
            ListTag entries = new() { ElementType = TagType.Compound };
            foreach (var block in Type.DiggerBlocks.OrderBy(block => block.Identifier, StringComparer.Ordinal))
                entries.Value.Add(new CompoundTag()
                    .Add("block", new CompoundTag()
                        .Add("name", new StringTag { Value = block.Identifier })
                        .Add("states", new CompoundTag())
                        .Add("tags", new StringTag { Value = string.Empty }))
                    .Add("speed", new IntTag { Value = (int)Type.MiningSpeed }));
            Type.PropertiesPayload.Values["minecraft:digger"] = new CompoundTag()
                .Add("destroy_speeds", entries)
                .Add("use_efficiency", new ByteTag { Value = 1 });
        }
        if (Type.FoodConvertsTo is { } result && ItemPalette.Find(result) is null)
            throw new InvalidOperationException($"Item '{Type.Identifier}' has an unknown food conversion: {result}.");
        if (Type.MaxDurability > 0 && Type.MaxAmount != 1)
            throw new InvalidOperationException($"Item '{Type.Identifier}' has durability and must have stack size 1.");
        if (Type.Catalog?.GroupIdentifier?.Icon is { } icon && ItemPalette.Find(icon) is null)
            throw new InvalidOperationException($"Item '{Type.Identifier}' has an unknown creative icon: {icon}.");
    }

    private CustomItem SetProperty(string name, Tag value) {
        Mutable();
        properties.Add(name, value);
        return this;
    }

    private void Mutable() {
        if (Environment.CurrentManagedThreadId != ownerThread || ItemPalette.Frozen)
            throw new InvalidOperationException("Configure items on their creating thread before starting the server.");
        ItemPalette.Invalidate();
    }
}
