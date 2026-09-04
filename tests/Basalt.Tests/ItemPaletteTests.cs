namespace Basalt.Tests;

using Basalt.Core.Item;
using Basalt.Core.Item.Traits;
using Basalt.BedrockProtocol.NBT;

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
}
