using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Events.Players;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Network.Handlers;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class PlayerSurvivalTests {
    [Fact]
    public void SprintingSpendsSaturationBeforeHunger() {
        using MultiplayerFixture fixture = new();
        Player player = fixture.CreatePlayer();
        PlayerInventoryTrait inventory = new(player);
        PlayerHungerTrait hunger = new(player);
        player.AddTrait(inventory);
        player.AddTrait(hunger);
        hunger.OnSpawn();
        player.Attributes.SetCurrent("minecraft:player.hunger", 10);
        inventory.Inventory.SetItem(0, new ItemStack("minecraft:apple"));
        Assert.True(hunger.Eat(inventory.Inventory.GetItem(0)!));
        player.Flags.Set(ActorFlag.Sprinting, true);
        player.Position.X += 40;
        player.OnMove();
        Assert.Equal(4, hunger.Exhaustion);

        player.SimulationAge = 1;
        hunger.Tick();

        Assert.Equal(14, hunger.Hunger);
        Assert.Equal(0, hunger.Exhaustion);
        Assert.Equal(1.4f, hunger.Saturation, 2);
    }

    [Fact]
    public void EatingAppliesEmbeddedFoodValuesAndConsumesOneItem() {
        using MultiplayerFixture fixture = new();
        Player player = fixture.CreatePlayer();
        PlayerInventoryTrait inventory = new(player);
        PlayerHungerTrait hunger = new(player);
        player.AddTrait(inventory);
        player.AddTrait(hunger);
        hunger.OnSpawn();
        inventory.Inventory.SetItem(0, new ItemStack("minecraft:apple", 2));
        hunger.Reset();
        player.Attributes.SetCurrent("minecraft:player.hunger", 10);
        inventory.SelectedSlot = 0;

        Assert.True(hunger.Eat(inventory.Inventory.GetItem(0)!));

        Assert.Equal(14, hunger.Hunger);
        Assert.Equal(2.4f, hunger.Saturation, 2);
        Assert.Equal((ushort)1, inventory.Inventory.GetItem(0)?.StackSize);
    }

    [Theory]
    [InlineData("minecraft:carrot", 3)]
    [InlineData("minecraft:bread", 5)]
    public void EatingLegacyFoodAppliesNutritionAndConsumesOneItem(string identifier, int nutrition) {
        using MultiplayerFixture fixture = new();
        Player player = fixture.CreatePlayer();
        PlayerInventoryTrait inventory = new(player);
        PlayerHungerTrait hunger = new(player);
        player.AddTrait(inventory);
        player.AddTrait(hunger);
        hunger.OnSpawn();
        inventory.Inventory.SetItem(0, new ItemStack(identifier, 2));
        inventory.SelectedSlot = 0;
        player.Attributes.SetCurrent("minecraft:player.hunger", 10);

        Assert.True(hunger.Eat(inventory.Inventory.GetItem(0)!));

        Assert.Equal(10 + nutrition, hunger.Hunger);
        Assert.Equal((ushort)1, inventory.Inventory.GetItem(0)?.StackSize);
    }

    [Fact]
    public void StartUsingItemDoesNotConsumeFood() {
        using MultiplayerFixture fixture = new();
        Player player = fixture.CreatePlayer();
        PlayerInventoryTrait inventory = new(player);
        PlayerHungerTrait hunger = new(player);
        player.AddTrait(inventory);
        player.AddTrait(hunger);
        player.AddTrait(new EntityMovementTrait(player));
        hunger.OnSpawn();
        inventory.Inventory.SetItem(0, new ItemStack("minecraft:bread", 2));
        inventory.SelectedSlot = 0;
        player.Attributes.SetCurrent("minecraft:player.hunger", 10);

        PlayerAuthInputPacket packet = new() {
            Position = player.GetHeadPosition(),
            InputData = [PlayerAuthInputData.StartUsingItem]
        };
        packet.ClientTick.Tick = 1;

        new PlayerAuthInputHandler().Handle(player.Peer, player, packet);

        Assert.Equal(10, hunger.Hunger);
        Assert.Equal((ushort)2, inventory.Inventory.GetItem(0)?.StackSize);
        Assert.False(player.Flags.Get(ActorFlag.UsingItem));
    }

    [Fact]
    public void UseItemConsumesFoodWhenAlreadyUsingIt() {
        using MultiplayerFixture fixture = new();
        Player player = fixture.CreatePlayer();
        PlayerInventoryTrait inventory = new(player);
        PlayerHungerTrait hunger = new(player);
        player.AddTrait(inventory);
        player.AddTrait(hunger);
        hunger.OnSpawn();
        ItemStack bread = new("minecraft:bread", 2);
        inventory.Inventory.SetItem(0, bread);
        inventory.SelectedSlot = 0;
        player.Attributes.SetCurrent("minecraft:player.hunger", 10);

        ItemUseInventoryTransaction use = new() {
            ActionType = ItemUseInventoryTransactionActionType.Use,
            Slot = 0,
            Hand = HandSlot.Mainhand,
            Item = bread.ToNetworkStackDescriptor(),
            FromPosition = new Vec3 { X = player.Position.X, Y = player.Position.Y, Z = player.Position.Z }
        };

        InventoryTransactionHandler.ProcessTransaction(player, use, 0, null);
        Assert.Equal(10, hunger.Hunger);
        Assert.Equal((ushort)2, inventory.Inventory.GetItem(0)?.StackSize);
        Assert.True(player.Flags.Get(ActorFlag.UsingItem));

        InventoryTransactionHandler.ProcessTransaction(player, use, 0, null);

        Assert.Equal(15, hunger.Hunger);
        Assert.Equal((ushort)1, inventory.Inventory.GetItem(0)?.StackSize);
        Assert.False(player.Flags.Get(ActorFlag.UsingItem));
    }

    [Theory]
    [InlineData(ItemReleaseInventoryTransactionActionType.Release)]
    [InlineData(ItemReleaseInventoryTransactionActionType.Use)]
    public void ItemReleaseDoesNotConsumeFood(ItemReleaseInventoryTransactionActionType actionType) {
        using MultiplayerFixture fixture = new();
        Player player = fixture.CreatePlayer();
        PlayerInventoryTrait inventory = new(player);
        PlayerHungerTrait hunger = new(player);
        player.AddTrait(inventory);
        player.AddTrait(hunger);
        hunger.OnSpawn();
        ItemStack bread = new("minecraft:bread", 2);
        inventory.Inventory.SetItem(0, bread);
        inventory.SelectedSlot = 0;
        player.Attributes.SetCurrent("minecraft:player.hunger", 10);
        player.SetFlag(ActorFlag.UsingItem, true);

        InventoryTransactionHandler.ProcessTransaction(player, new ItemReleaseInventoryTransaction {
            ActionType = actionType,
            Slot = 0,
            Item = bread.ToNetworkStackDescriptor(),
            Hand = HandSlot.Mainhand
        }, 0, null);

        Assert.Equal(10, hunger.Hunger);
        Assert.Equal((ushort)2, inventory.Inventory.GetItem(0)?.StackSize);
        Assert.Equal(actionType != ItemReleaseInventoryTransactionActionType.Release, player.Flags.Get(ActorFlag.UsingItem));
    }

    [Fact]
    public void CanceledEatingDoesNotConsumeFoodOrChangeHunger() {
        using MultiplayerFixture fixture = new();
        Player player = fixture.CreatePlayer();
        PlayerInventoryTrait inventory = new(player);
        PlayerHungerTrait hunger = new(player);
        player.AddTrait(inventory);
        player.AddTrait(hunger);
        hunger.OnSpawn();
        inventory.Inventory.SetItem(0, new ItemStack("minecraft:apple", 2));
        player.Attributes.SetCurrent("minecraft:player.hunger", 10);
        inventory.SelectedSlot = 0;
        fixture.Server.Events.Subscribe<PlayerEatEvent>(eat => eat.Cancel());

        Assert.False(hunger.Eat(inventory.Inventory.GetItem(0)!));

        Assert.Equal(10, hunger.Hunger);
        Assert.Equal((ushort)2, inventory.Inventory.GetItem(0)?.StackSize);
    }

    [Fact]
    public void FallDamageUsesLandingDistanceAndAppliesOnce() {
        using MultiplayerFixture fixture = new();
        Player player = fixture.CreatePlayer();
        EntityHealthTrait health = new(player);
        EntityMovementTrait movement = new(player);
        player.AddTrait(health);
        player.AddTrait(movement);
        player.Position.Y = 10;
        health.OnSpawn();

        player.Position.Y = 8;
        player.OnMove();
        player.Position.Y = 6;
        movement.VerticalCollision = true;
        player.OnMove();

        Assert.Equal(19, health.Current);
        Assert.Equal(0, health.FallDistance);
        player.OnMove();
        Assert.Equal(19, health.Current);
    }

    [Fact]
    public void SuffocationDamagesOnlyWhenHeadBlockIsSolid() {
        using MultiplayerFixture fixture = new();
        Player player = fixture.CreatePlayer();
        EntityHealthTrait health = new(player);
        player.AddTrait(health);
        player.Position.X = 0.5f;
        player.Position.Y = -57;
        fixture.Dimension.SetPermutation(0, -56, 0, BasaltBE.Basalt.Blocks.BlockPalette.FindDefaultPermutation("minecraft:stone")!);
        player.SimulationAge = 10;

        health.Tick();

        Assert.Equal(19, health.Current);
        player.Position.Y = -54;
        player.SimulationAge = 20;
        health.Tick();
        Assert.Equal(19, health.Current);
    }

    [Fact]
    public void RespawnRestoresHealthAndResetsPlayerTraits() {
        Player player;
        EntityHealthTrait health;
        PlayerHungerTrait hunger;
        using (MultiplayerFixture fixture = new()) {
            player = fixture.CreatePlayer();
            health = new EntityHealthTrait(player);
            hunger = new PlayerHungerTrait(player);
            player.AddTrait(health);
            player.AddTrait(hunger);
            player.OnSpawn();
            player.Attributes.SetCurrent("minecraft:health", 4);
            player.Attributes.SetCurrent("minecraft:player.hunger", 3);
            player.Dead = true;

            Assert.True(health.Respawn());
            player.OnRespawn();

            Assert.False(player.Dead);
            Assert.Equal(20, health.Current);
            Assert.Equal(20, hunger.Hunger);
            Assert.Equal(0, hunger.Saturation);
            Assert.Equal(0, health.FallDistance);
        }
    }
}
