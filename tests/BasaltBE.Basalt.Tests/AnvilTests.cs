using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Items.Enchantments;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Traits;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Network.Handlers;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class AnvilTests {
    private readonly EnchantmentRegistry enchantments = new();

    [Fact]
    public void EnchantedBookAppliesCompatibleEnchantmentsAndSkipsConflicts() {
        ItemStack target = new("minecraft:diamond_pickaxe");
        target.AddEnchantment(enchantments.Get("minecraft:silk_touch"), 1);
        ItemStack book = new("minecraft:enchanted_book");
        book.AddEnchantment(enchantments.Get("minecraft:fortune"), 2);
        book.AddEnchantment(enchantments.Get("minecraft:efficiency"), 3);

        ItemStack result = Assert.IsType<ItemStack>(AnvilContainer.CreateResult(target, book, enchantments, false, out int levelCost));

        Assert.Equal(1, result.GetEnchantmentLevel("minecraft:silk_touch"));
        Assert.Equal(0, result.GetEnchantmentLevel("minecraft:fortune"));
        Assert.Equal(3, result.GetEnchantmentLevel("minecraft:efficiency"));
        Assert.Equal(3, levelCost);
    }

    [Fact]
    public void EqualEnchantmentLevelsCombineUpToMaximum() {
        ItemStack target = new("minecraft:diamond_sword");
        target.AddEnchantment(enchantments.Get("minecraft:sharpness"), 4);
        ItemStack book = new("minecraft:enchanted_book");
        book.AddEnchantment(enchantments.Get("minecraft:sharpness"), 4);

        ItemStack result = Assert.IsType<ItemStack>(AnvilContainer.CreateResult(target, book, enchantments, false, out int levelCost));

        Assert.Equal(5, result.GetEnchantmentLevel("minecraft:sharpness"));
        Assert.Equal(1, levelCost);
    }

    [Fact]
    public void MatchingItemsMergeEqualEnchantmentLevels() {
        ItemStack target = new("minecraft:diamond_pickaxe");
        target.AddEnchantment(enchantments.Get("minecraft:efficiency"), 2);
        ItemStack material = new("minecraft:diamond_pickaxe");
        material.AddEnchantment(enchantments.Get("minecraft:efficiency"), 2);

        ItemStack result = Assert.IsType<ItemStack>(AnvilContainer.CreateResult(target, material, enchantments, false, out int levelCost));

        Assert.Equal(3, result.GetEnchantmentLevel("minecraft:efficiency"));
        Assert.Equal(1, levelCost);
    }

    [Fact]
    public void MatchingItemsCombineRemainingDurability() {
        ItemStack target = new("minecraft:iron_pickaxe");
        target.SetDamage(200);
        ItemStack material = new("minecraft:iron_pickaxe");
        material.SetDamage(100);

        ItemStack result = Assert.IsType<ItemStack>(AnvilContainer.CreateResult(target, material, enchantments, false, out int levelCost));

        int expectedDurability = Math.Min(
            target.Type.MaxDurability,
            target.RemainingDurability + material.RemainingDurability + target.Type.MaxDurability * 12 / 100
        );
        Assert.Equal(Math.Max(0, target.Type.MaxDurability - expectedDurability), result.Damage);
        Assert.Equal(2, levelCost);
        Assert.Equal(1, result.Nbt?.Get<IntTag>("RepairCost")?.Value);
    }

    [Fact]
    public void SurvivalCannotTakeAnvilResultAtFortyLevels() {
        ItemStack target = new("minecraft:diamond_sword");
        target.Nbt = new CompoundTag();
        target.Nbt.Add("RepairCost", new IntTag { Value = 39 });
        ItemStack book = new("minecraft:enchanted_book");
        book.AddEnchantment(enchantments.Get("minecraft:sharpness"), 1);

        Assert.Null(AnvilContainer.CreateResult(target, book, enchantments, false, out _));
        Assert.NotNull(AnvilContainer.CreateResult(target, book, enchantments, true, out int creativeCost));
        Assert.True(creativeCost >= 40);
    }

    [Fact]
    public void PaletteContainsCurrentBedrockEnchantments() {
        Assert.Equal(42, EnchantmentPalette.Enchantments.Length);
        Assert.Equal(42, enchantments.Values.Count);
    }

    [Fact]
    public void AnvilDamageAdvancesToChippedThenDamagedThenBreaks() {
        BlockPermutation anvil = Assert.IsType<BlockPermutation>(BlockPalette.FindDefaultPermutation("minecraft:anvil"));
        BlockPermutation chipped = Assert.IsType<BlockPermutation>(AnvilContainer.AdvanceDamage(anvil));
        BlockPermutation damaged = Assert.IsType<BlockPermutation>(AnvilContainer.AdvanceDamage(chipped));

        Assert.Equal("minecraft:chipped_anvil", chipped.Identifier);
        Assert.Equal("minecraft:damaged_anvil", damaged.Identifier);
        Assert.Null(AnvilContainer.AdvanceDamage(damaged));
    }

    [Fact]
    public void UnsupportedAnvilFallsAndLandsAboveTheFloor() {
        Dimension dimension = new("overworld", new SuperflatGenerator());
        dimension.GetOrCreateChunk(0, 0);
        dimension.SetPermutation(0, 0, 0, Assert.IsType<BlockPermutation>(BlockPalette.FindDefaultPermutation("minecraft:stone")));
        dimension.SetPermutation(0, 5, 0, Assert.IsType<BlockPermutation>(BlockPalette.FindDefaultPermutation("minecraft:anvil")));

        Assert.True(new AnvilTrait().OnNeighborUpdate(dimension, 0, 5, 0));
        Assert.Equal("minecraft:air", dimension.GetPermutation(0, 5, 0).Identifier);
        for (int tick = 0; tick < 200 && dimension.GetEntities().Length > 0; tick++)
            foreach (Entity entity in dimension.GetEntities())
                entity.Tick();

        Assert.Contains(
            dimension.GetPermutation(0, 1, 0).Identifier,
            new[] { "minecraft:anvil", "minecraft:chipped_anvil", "minecraft:damaged_anvil" }
        );
    }

    [Fact]
    public void TakingReportedPreviewConsumesInputsAndExperience() {
        using MultiplayerFixture fixture = new();
        Player player = fixture.CreatePlayer();
        PlayerInventoryTrait inventory = new(player);
        PlayerExperienceTrait experience = new(player);
        player.AddTrait(inventory);
        player.AddTrait(experience);
        experience.SetLevels(10);
        AnvilContainer anvil = new(player, new BlockPos());
        player.OpenedContainer = anvil;
        ItemStack sword = new("minecraft:diamond_sword");
        ItemStack book = new("minecraft:enchanted_book");
        book.AddEnchantment(enchantments.Get("minecraft:sharpness"), 1);
        anvil.SetItem(0, sword);
        anvil.SetItem(1, book);
        anvil.Refresh();
        ItemStack preview = Assert.IsType<ItemStack>(anvil.GetItem(2));

        ItemStackResponseInfo response = ItemStackRequestHandler.ProcessRequest(player, new ItemStackRequestData {
            ClientRequestId = 1,
            Actions = [
                new CraftNonImplementedActionData(),
                new CraftResultsActionData {
                    NumberOfCrafts = 1,
                    CraftResults = [new NetworkItemInstanceDescriptorData {
                        ItemDescriptor = new ItemNameDescriptorData {
                            FullName = preview.Type.Identifier,
                            AuxValue = 0x7FFF
                        },
                        StackSize = preview.StackSize
                    }]
                },
                new TakeActionData {
                    Amount = 1,
                    Source = new ItemStackSlotInfoData {
                        ContainerName = new FullContainerName { ContainerName = ContainerEnumName.CreatedOutput },
                        NetIdVariant = preview.NetId
                    },
                    Destination = new ItemStackSlotInfoData {
                        ContainerName = new FullContainerName { ContainerName = ContainerEnumName.Cursor }
                    }
                }
            ]
        });

        Assert.Equal(ItemStackNetResult.Success, response.Result);
        Assert.Equal(9, experience.Level);
        Assert.Null(anvil.GetItem(0));
        Assert.Null(anvil.GetItem(1));
        Assert.Equal(1, inventory.Cursor.GetItem(0)?.GetEnchantmentLevel("minecraft:sharpness"));
    }
}
