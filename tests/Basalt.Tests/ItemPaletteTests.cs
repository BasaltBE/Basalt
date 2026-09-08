namespace Basalt.Tests;

using Basalt.Binary;
using Basalt.Core.Item;
using Basalt.Core.Item.Traits;
using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public sealed class ItemPaletteTests {
    [Theory]
    [InlineData("minecraft:beetroot", 1, 0.6f)]
    [InlineData("minecraft:bread", 5, 0.6f)]
    public void FoodComponentsLoadNutritionAndSaturation(
        string identifier,
        int nutrition,
        float saturationModifier) {
        ItemType itemType = ItemPalette.ResolveType(identifier);
        ItemStackFoodTrait? food = new ItemStack(itemType).GetTrait<ItemStackFoodTrait>();

        Assert.NotNull(food);
        Assert.Equal(nutrition, food.Nutrition);
        Assert.Equal(saturationModifier, food.SaturationModifier, precision: 5);
    }

    [Fact]
    public void FOodTraitFixed() {
        ItemType itemType = ItemPalette.ResolveType("minecraft:bread");
        CompoundTag nbt = new();
        nbt.Set("nutrition", new IntTag { Value = 0 });
        nbt.Set("saturationModifier", new FloatTag { Value = 0f });

        ItemStackFoodTrait food = new ItemStack(itemType).GetTrait<ItemStackFoodTrait>()!;
        food.OnRead(nbt);

        Assert.Equal(5, food.Nutrition);
        Assert.Equal(0.6f, food.SaturationModifier, precision: 5);
    }

    [Fact]
    public void CreativeEnchantedBooksIncludeEnchantmentData() {
        byte[] payload = ItemPalette.GetCreativeContentPayload();
        int offset = 0;
        BinaryReader reader = new(payload, ref offset);
        CreativeContentPacket packet = new();
        packet.Deserialize(ref reader);

        ItemType enchantedBook = ItemPalette.ResolveType("minecraft:enchanted_book");
        CreativeItemEntryPayload[] entries = [
            .. packet.Entries.Where(entry => entry.ItemInstance.Id == enchantedBook.NetworkId)
        ];
        CreativeItemEntryPayload[] enchantedEntries = [
            .. entries.Where(entry =>
                Convert.FromBase64String(entry.ItemInstance.UserDataBuffer).Length > 4)
        ];

        Assert.NotEmpty(entries);
        Assert.NotEmpty(enchantedEntries);

        CreativeItemStack item = enchantedEntries[0].ItemInstance;
        byte[] userData = Convert.FromBase64String(item.UserDataBuffer);
        int nbtOffset = 3;
        BinaryReader nbtReader = new(userData, ref nbtOffset);
        CompoundTag nbt = NBT.ReadTag<CompoundTag>(
            nbtReader,
            new TagOptions(Name: true, Type: true, VarInt: false));

        Assert.NotNull(nbt.Get<ListTag>("ench"));

        ItemStack creativeItem = ItemPalette.GetCreativeItem((uint)enchantedEntries[0].CreativeNetId)!;
        Assert.NotEmpty(creativeItem.GetTrait<ItemStackEnchantmentTrait>()!.Enchantments);
    }

    [Fact]
    public void AnvilEnchantmentRulesUseItemTypes() {
        ItemStack pickaxe = new("minecraft:diamond_pickaxe");
        ItemStack helmet = new("minecraft:diamond_helmet");

        Assert.True(Basalt.Core.Item.Enchantment.EnchantmentType.Get("efficiency")!.CanApplyTo(pickaxe));
        Assert.False(Basalt.Core.Item.Enchantment.EnchantmentType.Get("protection")!.CanApplyTo(pickaxe));
        Assert.True(Basalt.Core.Item.Enchantment.EnchantmentType.Get("protection")!.CanApplyTo(helmet));
        Assert.True(Basalt.Core.Item.Enchantment.EnchantmentType.Get("sharpness")!
            .ConflictsWith(Basalt.Core.Item.Enchantment.EnchantmentType.Get("smite")!));
        Assert.False(Basalt.Core.Item.Enchantment.EnchantmentType.Get("efficiency")!
            .ConflictsWith(Basalt.Core.Item.Enchantment.EnchantmentType.Get("fortune")!));
    }
}
