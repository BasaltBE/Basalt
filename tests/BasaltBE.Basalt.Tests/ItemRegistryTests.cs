using BasaltBE.Basalt.Items;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Types;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class ItemRegistryTests {
    [Theory]
    [InlineData("minecraft:carrot", 3, 1, 32, false)]
    [InlineData("minecraft:bread", 5, 1, 32, false)]
    [InlineData("minecraft:honey_bottle", 6, 2, 40, false)]
    [InlineData("minecraft:apple", 4, 1, 32, true)]
    public void FoodRegistryEntriesIncludeFoodAndUseProperties(string identifier, int nutrition, int animation, int duration, bool componentBased) {
        ItemData entry = Assert.Single(ItemPalette.RegistryPacket.Items, item => item.Name == identifier);
        CompoundTag components = Assert.IsType<CompoundTag>(entry.ComponentData.Get<CompoundTag>("components"));
        CompoundTag itemProperties = Assert.IsType<CompoundTag>(components.Get<CompoundTag>("item_properties"));
        CompoundTag food = Assert.IsType<CompoundTag>(components.Get<CompoundTag>("minecraft:food"));

        Assert.Equal(componentBased, entry.ComponentBased);
        Assert.Equal(nutrition, food.Get<IntTag>("nutrition")?.Value);
        Assert.Equal(animation, itemProperties.Get<IntTag>("use_animation")?.Value);
        Assert.Equal(duration, itemProperties.Get<IntTag>("use_duration")?.Value);
        if (!componentBased)
            Assert.Equal(duration, Assert.IsType<IntTag>(components.Get<IntTag>("minecraft:use_duration")).Value);
    }
}
