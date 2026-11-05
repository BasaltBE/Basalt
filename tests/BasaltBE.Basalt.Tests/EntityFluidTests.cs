using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Events.Entities;
using BasaltBE.Basalt.Network.Handlers;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class EntityFluidTests {
    [Fact]
    public void DroppedItemRisesInStillWater() {
        using MultiplayerFixture fixture = new();
        for (int y = 0; y < 4; y++)
            fixture.Dimension.SetPermutation(0, y, 0, BlockPalette.FindDefaultPermutation("minecraft:water")!);
        ItemEntity item = new(new ItemStack("minecraft:stone"), 0, 0, 0);
        item.Spawn(fixture.Dimension);
        EntityMovementTrait movement = item.GetTrait<EntityMovementTrait>()!;
        movement.Motion.X = movement.Motion.Y = movement.Motion.Z = 0;
        float start = item.Position.Y;

        for (int tick = 0; tick < 60; tick++)
            item.Tick();

        Assert.True(item.Position.Y > start, $"Item sank to {item.Position.Y} from {start}.");
        Assert.InRange(movement.Motion.Y, 0, 0.061f);
    }

    [Theory]
    [InlineData("minecraft:water", -0.02f, 0.8f)]
    [InlineData("minecraft:lava", -0.02f, 0.5f)]
    public void LivingEntityUsesFluidDragAndGravity(string identifier, float vertical, float horizontal) {
        using MultiplayerFixture fixture = new();
        for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
                fixture.Dimension.SetPermutation(x, y, 0, BlockPalette.FindDefaultPermutation(identifier)!);
        Entity cow = new("minecraft:cow");
        EntityMovementTrait movement = new(cow);
        cow.AddTrait(movement);
        cow.Position.X = cow.Position.Z = 0.5f;
        cow.Spawn(fixture.Dimension);
        movement.Motion.X = 1;

        cow.Tick();

        Assert.Equal(horizontal, movement.Motion.X, 4);
        Assert.Equal(vertical, movement.Motion.Y, 4);
        Assert.False(movement.Grounded);
    }

    [Fact]
    public void CurrentPushesTowardsLowerWaterAndNotIntoAnUnloadedChunk() {
        using MultiplayerFixture fixture = new();
        fixture.Dimension.SetPermutation(14, 0, 0, BlockPalette.FindDefaultPermutation("minecraft:water")!);
        BlockPermutation water = BlockPalette.FindDefaultPermutation("minecraft:flowing_water")!;
        fixture.Dimension.SetPermutation(15, 0, 0, BlockPalette.Find(water.Identifier, water.State, "liquid_depth", 1)!);
        ItemEntity item = new(new ItemStack("minecraft:stone"), 14, 0, 0);
        item.Spawn(fixture.Dimension);
        EntityMovementTrait movement = item.GetTrait<EntityMovementTrait>()!;
        movement.Motion.X = movement.Motion.Y = movement.Motion.Z = 0;

        item.Tick();

        Assert.True(movement.Motion.X > 0);
        Assert.Equal(0, movement.Motion.Z);
        Assert.Null(fixture.Dimension.GetChunk(1, 0));
    }

    [Fact]
    public void WaterloggedLayerPushesItems() {
        using MultiplayerFixture fixture = new();
        fixture.Dimension.SetPermutation(0, 0, 0, BlockPalette.FindDefaultPermutation("minecraft:water")!, 1);
        ItemEntity item = new(new ItemStack("minecraft:stone"), 0, 0, 0);
        item.Spawn(fixture.Dimension);
        EntityMovementTrait movement = item.GetTrait<EntityMovementTrait>()!;
        movement.Motion.X = movement.Motion.Y = movement.Motion.Z = 0;

        item.Tick();

        Assert.True(movement.Motion.Y > 0);
        Assert.Equal(0, movement.Motion.X);
        Assert.Equal(0, movement.Motion.Z);
    }

    [Fact]
    public void FallingWaterAtWallPullsDown() {
        using MultiplayerFixture fixture = new();
        BlockPermutation water = BlockPalette.FindDefaultPermutation("minecraft:flowing_water")!;
        fixture.Dimension.SetPermutation(0, 0, 0, BlockPalette.Find(water.Identifier, water.State, "liquid_depth", 8)!);
        fixture.Dimension.SetPermutation(1, 0, 0, BlockPalette.FindDefaultPermutation("minecraft:stone")!);
        ItemEntity item = new(new ItemStack("minecraft:stone"), 0, 0, 0);
        item.Spawn(fixture.Dimension);
        EntityMovementTrait movement = item.GetTrait<EntityMovementTrait>()!;
        movement.Motion.X = movement.Motion.Y = movement.Motion.Z = 0;

        item.Tick();

        Assert.True(movement.Motion.Y < 0);
    }

    [Fact]
    public void FluidAtFeetDoesNotSubmergeTheEyes() {
        using MultiplayerFixture fixture = new();
        fixture.Dimension.SetPermutation(0, 0, 0, BlockPalette.FindDefaultPermutation("minecraft:water")!);
        Entity cow = new("minecraft:cow");
        cow.Position.X = cow.Position.Z = 0.5f;
        cow.Spawn(fixture.Dimension);

        FluidContact contact = cow.GetFluidContact();

        Assert.True(contact.WaterDepth > 0);
        Assert.False(contact.HeadInWater);
    }

    [Fact]
    public void WaterAboveMakesTheLowerBlockFullHeight() {
        using MultiplayerFixture fixture = new();
        fixture.Dimension.SetPermutation(0, 0, 0, BlockPalette.FindDefaultPermutation("minecraft:water")!);
        fixture.Dimension.SetPermutation(0, 1, 0, BlockPalette.FindDefaultPermutation("minecraft:water")!);
        ItemEntity item = new(new ItemStack("minecraft:stone"), 0, 0, 0);
        item.Position.Y = 0.9f;
        item.Spawn(fixture.Dimension);

        Assert.True(item.GetFluidContact().WaterDepth > 0);
    }

    [Theory]
    [InlineData("minecraft:cow", 8)]
    [InlineData("minecraft:cod", 6)]
    [InlineData("minecraft:zombie", 20)]
    public void BreathableDataControlsDrowning(string identifier, float expectedHealth) {
        using MultiplayerFixture fixture = new();
        for (int y = 0; y < 4; y++)
            fixture.Dimension.SetPermutation(0, y, 0, BlockPalette.FindDefaultPermutation("minecraft:water")!);
        Entity entity = new(identifier);
        EntityHealthTrait health = new(entity);
        entity.AddTrait(health);
        entity.Position.X = entity.Position.Z = 0.5f;
        entity.Spawn(fixture.Dimension);
        for (int tick = 0; tick < 319; tick++)
            entity.Tick();
        if (identifier == "minecraft:cow")
            Assert.Equal(10, health.Current);

        entity.Tick();

        Assert.Equal(expectedHealth, health.Current);
    }

    [Fact]
    public void DrowningCanBeCanceledAndAirSurvivesSaving() {
        using MultiplayerFixture fixture = new();
        for (int y = 0; y < 3; y++)
            fixture.Dimension.SetPermutation(0, y, 0, BlockPalette.FindDefaultPermutation("minecraft:water")!);
        Entity cow = new("minecraft:cow");
        EntityHealthTrait health = new(cow);
        cow.AddTrait(health);
        cow.Position.X = cow.Position.Z = 0.5f;
        cow.Spawn(fixture.Dimension);
        fixture.Server.Events.Subscribe<EntityHurtEvent>(hurt => {
            Assert.Equal(EntityDamageCause.Drowning, hurt.Cause);
            hurt.Cancel();
        });
        for (int tick = 0; tick < 320; tick++)
            cow.Tick();

        Assert.Equal(10, health.Current);
        Assert.Equal(0, health.AirSupply);
        CompoundTag saved = new();
        cow.Write(saved);
        Assert.Equal((short)0, saved.Get<ShortTag>("Air")!.Value);
        health.OnRespawn();
        Assert.Equal(300, health.AirSupply);
        cow.Read(saved);
        Assert.Equal(0, health.AirSupply);
        cow.Position.Y = 6;
        cow.OnMove();
        cow.Tick();
        Assert.Equal(300, health.AirSupply);
        Assert.True(cow.Flags.Get(ActorFlag.Breathing));
    }

    [Fact]
    public void PlayerAirRefillsOverTimeAndCreativeDoesNotDrown() {
        using MultiplayerFixture fixture = new();
        for (int y = 0; y < 3; y++)
            fixture.Dimension.SetPermutation(0, y, 0, BlockPalette.FindDefaultPermutation("minecraft:water")!);
        Player player = fixture.CreatePlayer();
        EntityHealthTrait health = new(player);
        player.AddTrait(health);
        player.Position.X = player.Position.Z = 0.5f;
        player.Position.Y = 0;
        for (int tick = 0; tick < 300; tick++) {
            player.SimulationAge++;
            health.Tick();
        }
        Assert.Equal(0, health.AirSupply);
        player.Position.Y = 6;
        player.OnMove();
        player.SimulationAge++;
        health.Tick();
        Assert.Equal(4, health.AirSupply);
        player.Position.Y = 0;
        player.Abilities.Set(PlayerAbility.Invulnerable, true);
        player.OnMove();
        for (int tick = 0; tick < 400; tick++) {
            player.SimulationAge++;
            health.Tick();
        }
        Assert.Equal(20, health.Current);
        Assert.Equal(300, health.AirSupply);
    }

    [Fact]
    public void EnteringWaterResetsFallDistanceWithoutSwimming() {
        using MultiplayerFixture fixture = new();
        Player player = fixture.CreatePlayer();
        EntityHealthTrait health = new(player);
        EntityMovementTrait movement = new(player);
        player.AddTrait(health);
        player.AddTrait(movement);
        fixture.Dimension.SetPermutation(0, 0, 0, BlockPalette.FindDefaultPermutation("minecraft:water")!);
        player.Position.X = player.Position.Z = 0.5f;
        player.Position.Y = 20;
        health.OnSpawn();
        player.Position.Y = 10;
        player.OnMove();
        Assert.True(health.FallDistance > 3);
        player.Position.Y = 0.5f;
        movement.VerticalCollision = true;

        player.OnMove();

        Assert.Equal(20, health.Current);
        Assert.Equal(0, health.FallDistance);
        Assert.False(player.Flags.Get(ActorFlag.Swimming));
    }

    [Theory]
    [InlineData("minecraft:oak_slab", "bottom", 0.5f)]
    [InlineData("minecraft:oak_slab", "top", 1)]
    [InlineData("minecraft:farmland", "", 0.9375f)]
    [InlineData("minecraft:grass_path", "", 0.9375f)]
    public void ItemLandsOnPartialBlockSurface(string identifier, string half, float expectedY) {
        using MultiplayerFixture fixture = new();
        BlockPermutation block = BlockPalette.FindDefaultPermutation(identifier)!;
        if (half.Length > 0)
            block = BlockPalette.Find(identifier, block.State, "minecraft:vertical_half", half)!;
        fixture.Dimension.SetPermutation(0, 0, 0, block);
        ItemEntity item = new(new ItemStack("minecraft:stone"), 0, 3, 0);
        item.Spawn(fixture.Dimension);
        EntityMovementTrait movement = item.GetTrait<EntityMovementTrait>()!;
        movement.Motion.X = movement.Motion.Y = movement.Motion.Z = 0;
        for (int tick = 0; tick < 60; tick++)
            item.Tick();

        Assert.Equal(expectedY, item.Position.Y, 4);
        Assert.Equal(0, movement.Motion.Y);
    }

    [Theory]
    [InlineData(false, 0.25f, 0.5f)]
    [InlineData(false, 0.75f, 1)]
    [InlineData(true, 0.25f, 1)]
    [InlineData(true, 0.75f, 1)]
    public void StairCollisionUsesTheStepAndHalf(bool upper, float x, float expectedY) {
        using MultiplayerFixture fixture = new();
        BlockPermutation stair = BlockPalette.FindDefaultPermutation("minecraft:oak_stairs")!;
        stair = BlockPalette.Find(stair.Identifier, stair.State, "weirdo_direction", 0)!;
        stair = BlockPalette.Find(stair.Identifier, stair.State, "upside_down_bit", upper)!;
        fixture.Dimension.SetPermutation(0, 0, 0, stair);
        ItemEntity item = new(new ItemStack("minecraft:stone"), 0, 3, 0);
        item.Position.X = x;
        item.Spawn(fixture.Dimension);
        EntityMovementTrait movement = item.GetTrait<EntityMovementTrait>()!;
        movement.Motion.X = movement.Motion.Y = movement.Motion.Z = 0;
        for (int tick = 0; tick < 60; tick++)
            item.Tick();

        Assert.Equal(expectedY, item.Position.Y, 4);
    }

    [Fact]
    public void FastItemCannotTunnelThroughWall() {
        using MultiplayerFixture fixture = new();
        fixture.Dimension.SetPermutation(3, 2, 0, BlockPalette.FindDefaultPermutation("minecraft:stone")!);
        ItemEntity item = new(new ItemStack("minecraft:stone"), 0, 2, 0);
        item.Spawn(fixture.Dimension);
        item.Flags.Set(ActorFlag.HasGravity, false);
        EntityMovementTrait movement = item.GetTrait<EntityMovementTrait>()!;
        movement.Motion.X = 5;
        movement.Motion.Y = movement.Motion.Z = 0;

        item.Tick();

        Assert.Equal(2.875f, item.Position.X, 4);
        Assert.Equal(0, movement.Motion.X);
        Assert.True(movement.HorizontalCollision);
    }

    [Fact]
    public void PlayerMovementInWaterIsNotSimulatedTwice() {
        using MultiplayerFixture fixture = new();
        Player player = fixture.CreatePlayer();
        EntityMovementTrait movement = new(player);
        player.AddTrait(movement);
        player.Position.X = player.Position.Z = 0.5f;
        player.Position.Y = 0;
        for (int y = 0; y < 3; y++)
            fixture.Dimension.SetPermutation(0, y, 0, BlockPalette.FindDefaultPermutation("minecraft:water")!);
        PlayerAuthInputPacket packet = new() {
            Position = new() { X = 0.6f, Y = player.HeadOffset, Z = 0.5f },
            InputData = [PlayerAuthInputData.StartSwimming]
        };
        packet.ClientTick.Tick = 1;
        new PlayerAuthInputHandler().Handle(player.Peer, player, packet);

        movement.Tick();

        Assert.Equal(0.6f, player.Position.X);
        Assert.Equal(0, player.Position.Y);
        Assert.True(player.Flags.Get(ActorFlag.Swimming));
        Assert.Equal(0.6f, player.GetCollisionHeight());
    }
    [Theory]
    [InlineData("outer_left", 0.75f, 0.75f, 0.5f)]
    [InlineData("outer_left", 0.75f, 0.25f, 1)]
    [InlineData("outer_right", 0.75f, 0.25f, 0.5f)]
    [InlineData("inner_left", 0.25f, 0.25f, 1)]
    [InlineData("inner_right", 0.25f, 0.75f, 1)]
    public void StairCornersHaveQuarterBlockCollision(string corner, float x, float z, float expectedY) {
        using MultiplayerFixture fixture = new();
        BlockPermutation stair = BlockPalette.FindDefaultPermutation("minecraft:oak_stairs")!;
        stair = BlockPalette.Find(stair.Identifier, stair.State, "weirdo_direction", 0)!;
        stair = BlockPalette.Find(stair.Identifier, stair.State, "upside_down_bit", false)!;
        stair = BlockPalette.Find(stair.Identifier, stair.State, "minecraft:corner", corner)!;
        fixture.Dimension.SetPermutation(0, 0, 0, stair);
        ItemEntity item = new(new ItemStack("minecraft:stone"), 0, 3, 0);
        item.Position.X = x;
        item.Position.Z = z;
        item.Spawn(fixture.Dimension);
        EntityMovementTrait movement = item.GetTrait<EntityMovementTrait>()!;
        movement.Motion.X = movement.Motion.Y = movement.Motion.Z = 0;
        for (int tick = 0; tick < 60; tick++)
            item.Tick();

        Assert.Equal(expectedY, item.Position.Y, 4);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LivingEntityStepsOntoSlabOnlyWithHeadroom(bool ceiling) {
        using MultiplayerFixture fixture = new();
        for (int x = 0; x < 3; x++)
            fixture.Dimension.SetPermutation(x, -1, 0, BlockPalette.FindDefaultPermutation("minecraft:stone")!);
        BlockPermutation slab = BlockPalette.FindDefaultPermutation("minecraft:oak_slab")!;
        fixture.Dimension.SetPermutation(1, 0, 0, BlockPalette.Find(slab.Identifier, slab.State, "minecraft:vertical_half", "bottom")!);
        if (ceiling)
            fixture.Dimension.SetPermutation(1, 2, 0, BlockPalette.FindDefaultPermutation("minecraft:stone")!);
        Entity cow = new("minecraft:cow") { CollisionHeight = 1.8f };
        EntityMovementTrait movement = new(cow);
        cow.AddTrait(movement);
        cow.Position.X = cow.Position.Z = 0.5f;
        cow.Spawn(fixture.Dimension);
        cow.Tick();
        movement.Motion.X = 0.6f;

        cow.Tick();

        if (ceiling) {
            Assert.Equal(0, cow.Position.Y, 4);
            Assert.InRange(cow.Position.X, 0.5f, 0.551f);
        }
        else {
            Assert.Equal(0.5f, cow.Position.Y, 4);
            Assert.True(cow.Position.X > 0.55f);
        }
    }

    [Fact]
    public void AirMetadataUsesBedrockShortFields() {
        using MultiplayerFixture fixture = new();
        Player player = fixture.CreatePlayer();
        player.AddTrait(new EntityHealthTrait(player));
        var data = player.Flags.CreateSnapshot();
        var air = Assert.Single(data, item => item.Id == (uint)ActorDataId.AirSupply);
        var maximum = Assert.Single(data, item => item.Id == (uint)ActorDataId.MaxAirSupply);
        Assert.Equal(DataItemType.Short, air.Type);
        Assert.Equal((short)300, Assert.IsType<short>(air.Value));
        Assert.Equal((short)300, Assert.IsType<short>(maximum.Value));
    }

    [Fact]
    public void WaterExtinguishesWhileLavaStillDamagesAndIgnites() {
        using MultiplayerFixture fixture = new();
        fixture.Dimension.SetPermutation(0, 0, 0, BlockPalette.FindDefaultPermutation("minecraft:lava")!);
        Entity cow = new("minecraft:cow");
        EntityHealthTrait health = new(cow);
        cow.AddTrait(health);
        cow.Position.X = cow.Position.Z = 0.5f;
        cow.Spawn(fixture.Dimension);
        cow.Tick();
        Assert.Equal(6, health.Current);
        Assert.True(health.Burning);
        fixture.Dimension.SetPermutation(0, 0, 0, BlockPalette.FindDefaultPermutation("minecraft:water")!);

        cow.Tick();

        Assert.False(health.Burning);
        Assert.Equal(6, health.Current);
    }

    [Theory]
    [InlineData(0, 0, 0, 0.1825f, 1)]
    [InlineData(1, 0.8175f, 0, 1, 1)]
    [InlineData(2, 0, 0, 1, 0.1825f)]
    [InlineData(3, 0, 0.8175f, 1, 1)]
    public void OpenTrapdoorCollisionUsesBedrockDirection(int direction, float minX, float minZ, float maxX, float maxZ) {
        BlockPermutation trapdoor = BlockPalette.FindDefaultPermutation("minecraft:trapdoor")!;
        trapdoor = BlockPalette.Find(trapdoor.Identifier, trapdoor.State, "direction", direction)!;
        trapdoor = BlockPalette.Find(trapdoor.Identifier, trapdoor.State, "open_bit", true)!;
        Span<BlockCollisionBox> boxes = stackalloc BlockCollisionBox[5];

        Assert.Equal(1, trapdoor.GetCollisionBoxes(boxes));
        Assert.Equal(new BlockCollisionBox(minX, 0, minZ, maxX, 1, maxZ), boxes[0]);
    }

    [Fact]
    public void FluidContactCacheSeesBlockChangesWithoutMoving() {
        using MultiplayerFixture fixture = new();
        Entity cow = new("minecraft:cow");
        cow.Position.X = cow.Position.Z = 4.5f;
        cow.Spawn(fixture.Dimension);
        fixture.Dimension.GetOrCreateChunk(0, 0);
        Assert.Equal(0, cow.GetFluidContact().WaterDepth);
        fixture.Dimension.SetPermutation(4, 0, 4, BlockPalette.FindDefaultPermutation("minecraft:water")!);

        Assert.True(cow.GetFluidContact().WaterDepth > 0);
        fixture.Dimension.SetPermutation(4, 0, 4, BlockPalette.FindDefaultPermutation("minecraft:air")!);
        Assert.Equal(0, cow.GetFluidContact().WaterDepth);
    }

    [Fact]
    public void FluidContactAtChunkBorderSeesNeighborChanges() {
        using MultiplayerFixture fixture = new();
        fixture.Dimension.SetPermutation(15, 0, 4, BlockPalette.FindDefaultPermutation("minecraft:water")!);
        fixture.Dimension.GetOrCreateChunk(1, 0);
        ItemEntity item = new(new ItemStack("minecraft:stone"), 15, 0, 4);
        item.Position.X = 15.8f;
        item.Spawn(fixture.Dimension);
        Assert.Equal(0, item.GetFluidContact().WaterFlow.X);
        BlockPermutation flowing = BlockPalette.FindDefaultPermutation("minecraft:flowing_water")!;
        fixture.Dimension.SetPermutation(16, 0, 4, BlockPalette.Find(flowing.Identifier, flowing.State, "liquid_depth", 1)!);

        Assert.True(item.GetFluidContact().WaterFlow.X > 0);
    }

    [Fact]
    public void NonLivingEntityKeepsItsGravityAndDragInWater() {
        using MultiplayerFixture fixture = new();
        for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
                fixture.Dimension.SetPermutation(x, y, 0, BlockPalette.FindDefaultPermutation("minecraft:water")!);
        Entity tnt = new("minecraft:tnt");
        EntityMovementTrait movement = new(tnt) { Gravity = 0.04f };
        tnt.AddTrait(movement);
        tnt.Position.X = tnt.Position.Z = 0.5f;
        tnt.Spawn(fixture.Dimension);
        movement.Motion.X = 1;

        tnt.Tick();

        Assert.Equal(0.98f, movement.Motion.X, 4);
        Assert.Equal(-0.0392f, movement.Motion.Y, 4);
    }

    [Theory]
    [InlineData("minecraft:oak_fence", 0.5f, 1.5f)]
    [InlineData("minecraft:fence_gate", 0.5f, 1.5f)]
    [InlineData("minecraft:glass_pane", 0.5f, 1)]
    [InlineData("minecraft:trapdoor", 0.5f, 0.1825f)]
    [InlineData("minecraft:wooden_door", 0.1f, 1)]
    [InlineData("minecraft:snow_layer", 0.5f, 0.875f)]
    public void OtherPartialBlocksUseTheirCollisionHeight(string identifier, float localZ, float expectedY) {
        using MultiplayerFixture fixture = new();
        BlockPermutation block = BlockPalette.FindDefaultPermutation(identifier)!;
        if (identifier == "minecraft:snow_layer")
            block = BlockPalette.Find(identifier, block.State, "height", 7)!;
        fixture.Dimension.SetPermutation(4, 0, 4, block);
        ItemEntity item = new(new ItemStack("minecraft:stone"), 4, 3, 4);
        item.Position.Z = 4 + localZ;
        item.Spawn(fixture.Dimension);
        EntityMovementTrait movement = item.GetTrait<EntityMovementTrait>()!;
        movement.Motion.X = movement.Motion.Y = movement.Motion.Z = 0;
        for (int tick = 0; tick < 60; tick++)
            item.Tick();

        Assert.Equal(expectedY, item.Position.Y, 4);
    }

    [Theory]
    [InlineData(ActorFlag.Sneaking, 1, 5)]
    [InlineData(ActorFlag.Swimming, 0, 4)]
    public void PlayerEyeImmersionFollowsPose(ActorFlag pose, int waterY, int depth) {
        using MultiplayerFixture fixture = new();
        Player player = fixture.CreatePlayer();
        player.Position.X = player.Position.Z = 4.5f;
        player.Position.Y = 0;
        BlockPermutation water = BlockPalette.FindDefaultPermutation("minecraft:flowing_water")!;
        fixture.Dimension.SetPermutation(4, waterY, 4, BlockPalette.Find(water.Identifier, water.State, "liquid_depth", depth)!);
        Assert.False(player.GetFluidContact().HeadInWater);

        player.Flags.Set(pose, true);

        Assert.True(player.GetFluidContact().HeadInWater);
        Assert.Equal(1.62f, player.HeadOffset);
    }

    [Fact]
    public void SubmergedItemDampsDownwardMomentum() {
        using MultiplayerFixture fixture = new();
        for (int y = 0; y < 3; y++)
            fixture.Dimension.SetPermutation(4, y, 4, BlockPalette.FindDefaultPermutation("minecraft:water")!);
        ItemEntity item = new(new ItemStack("minecraft:stone"), 4, 1, 4);
        item.Spawn(fixture.Dimension);
        EntityMovementTrait movement = item.GetTrait<EntityMovementTrait>()!;
        movement.Motion.X = movement.Motion.Z = 0;
        movement.Motion.Y = -0.2f;

        item.Tick();

        Assert.Equal(-0.19551f, movement.Motion.Y, 5);
    }

    [Fact]
    public void FallingItemReturnsToSurfaceOfThreeBlockWaterHole() {
        using MultiplayerFixture fixture = new();
        BlockPermutation stone = BlockPalette.FindDefaultPermutation("minecraft:stone")!;
        BlockPermutation water = BlockPalette.FindDefaultPermutation("minecraft:water")!;
        fixture.Dimension.SetPermutation(4, -1, 4, stone);
        for (int y = 0; y < 3; y++) {
            fixture.Dimension.SetPermutation(4, y, 4, water);
            fixture.Dimension.SetPermutation(3, y, 4, stone);
            fixture.Dimension.SetPermutation(5, y, 4, stone);
            fixture.Dimension.SetPermutation(4, y, 3, stone);
            fixture.Dimension.SetPermutation(4, y, 5, stone);
        }
        ItemEntity item = new(new ItemStack("minecraft:stone"), 4, 4, 4);
        item.Spawn(fixture.Dimension);
        EntityMovementTrait movement = item.GetTrait<EntityMovementTrait>()!;
        movement.Motion.X = movement.Motion.Z = 0;
        movement.Motion.Y = -0.35f;
        bool enteredWater = false;
        bool rose = false;
        for (int tick = 0; tick < 360; tick++) {
            item.Tick();
            enteredWater |= item.GetFluidContact().WaterDepth > 0;
            rose |= enteredWater && movement.Motion.Y > 0;
        }

        Assert.True(enteredWater);
        Assert.True(rose);
        Assert.InRange(item.Position.Y, 2.5f, 3.1f);
    }

    [Fact]
    public void ItemNearWaterSurfaceDoesNotResumeFullGravity() {
        using MultiplayerFixture fixture = new();
        fixture.Dimension.SetPermutation(4, 0, 4, BlockPalette.FindDefaultPermutation("minecraft:water")!);
        ItemEntity item = new(new ItemStack("minecraft:stone"), 4, 0, 4);
        item.Position.Y = 0.84f;
        item.Spawn(fixture.Dimension);
        EntityMovementTrait movement = item.GetTrait<EntityMovementTrait>()!;
        movement.Motion.X = movement.Motion.Y = movement.Motion.Z = 0;

        item.Tick();

        Assert.Equal(-0.0196f, movement.Motion.Y, 5);
    }

}
