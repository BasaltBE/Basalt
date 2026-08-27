namespace Basalt.Tests;

using Basalt.Core;
using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Worlds.Dimensions.Chunk;
using Basalt.Core.Worlds.Dimensions.Generation;
using Basalt.Core.Worlds.Dimensions.Provider;
using Basalt.Core.Worlds;
using Basalt.Core.Player;
using Basalt.Core.Traits;
using Basalt.Core.Enums;
using Basalt.BedrockProtocol.Types;

public sealed class DimensionPlayerOwnershipTests {
    [Fact]
    public void SimulationAreaUpdatesWhenPlayerChangesChunk() {
        Server server = new(new Properties {
            SimulationDistance = 1,
            WorldProvider = "memory"
        });

        try {
            Dimension dimension = server.GetWorld().GetDimension(DimensionId.Overworld)!;
            Chunk oldChunk = dimension.GetOrCreateChunk(0, 0);
            Chunk newChunk = dimension.GetOrCreateChunk(3, 0);
            Player player = new("Alex", "xuid", Guid.NewGuid()) {
                Position = new Vec3 { X = 8, Y = 80, Z = 8 }
            };
            player.Spawn(dimension, new EntitySpawnOptions());

            server.Tick();
            Assert.True(oldChunk.Simulated);
            Assert.False(newChunk.Simulated);

            player.Position = new Vec3 { X = 56, Y = 80, Z = 8 };
            server.Tick();

            Assert.False(oldChunk.Simulated);
            Assert.True(newChunk.Simulated);
        }
        finally {
            server.Stop();
        }
    }

    [Fact]
    public void SpawningPlayerAddsPlayerToDimension() {
        using EmptyProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());
        Player player = new("Alex", "xuid", Guid.NewGuid());

        player.Spawn(dimension, new EntitySpawnOptions());

        Assert.Contains(player, dimension.GetPlayers());
        Assert.Contains(player, dimension.Entities);
    }

    [Fact]
    public void RemovingPlayerRemovesPlayerFromDimension() {
        using EmptyProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());
        Player player = new("Alex", "xuid", Guid.NewGuid());
        player.Spawn(dimension, new EntitySpawnOptions());

        dimension.RemoveEntity(player);

        Assert.DoesNotContain(player, dimension.GetPlayers());
        Assert.DoesNotContain(player, dimension.Entities);
        Assert.Null(player.Dimension);
    }

    [Fact]
    public void WorldPlayerViewUsesDimensionPlayers() {
        using World world = new("test", new EmptyProvider());
        Dimension dimension = new("overworld", DimensionId.Overworld, world.Provider, new VoidGenerator());
        Player player = new("Alex", "xuid", Guid.NewGuid());
        world.AddDimension(dimension);

        player.Spawn(dimension, new EntitySpawnOptions());

        Assert.Contains(player, world.GetPlayers());
        Assert.Contains(player, world.GetPlayersSnapshot());
    }

    [Fact]
    public void DimensionTickDrainsMailboxCommands() {
        using EmptyProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());
        int value = 0;

        Assert.True(dimension.TryEnqueue(() => value = 1));

        dimension.Tick(1, 1);

        Assert.Equal(1, value);
        Assert.False(dimension.IsOwnerThread);
    }

    [Fact]
    public void MailboxCommandRunsOnTheDimensionOwnerThread() {
        using EmptyProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());
        bool owner = false;

        Assert.True(dimension.TryEnqueue(() => owner = dimension.IsOwnerThread));
        dimension.Tick(1, 1);

        Assert.True(owner);
    }

    [Fact]
    public void PublicDimensionQueueRunsOnTheOwner() {
        using EmptyProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());
        bool owner = false;

        Assert.True(dimension.TryEnqueue(() => owner = dimension.IsOwnerThread));
        dimension.Tick(1, 1);

        Assert.True(owner);
    }

    [Fact]
    public void PendingEntityChangesUpdateMembershipAtTickBoundary() {
        Server server = new(new Properties { WorldProvider = "memory" });
        try {
            Dimension dimension = server.GetWorld().GetDimension(DimensionId.Overworld)!;
            Player player = new("Alex", "xuid", Guid.NewGuid()) {
                Position = new Vec3 { X = 8, Y = 80, Z = 8 }
            };
            player.Spawn(dimension, new EntitySpawnOptions());

            Entity source = new("test:source") {
                Position = new Vec3 { X = 8, Y = 80, Z = 8 }
            };
            source.AddTrait(new SpawnAndDespawnTrait(source));
            source.Spawn(dimension, new EntitySpawnOptions());

            server.Tick();

            Entity[] entities = dimension.GetEntitiesSnapshot();
            Entity child = Assert.Single(entities, entity => entity.Identifier == "test:child");
            Assert.DoesNotContain(source, entities);
            Assert.Contains(child, dimension.GetEntities(0, 0));
            Assert.DoesNotContain(source, dimension.GetEntities(0, 0));
        }
        finally {
            server.Stop();
        }
    }

    [Fact]
    public void RegionTicksApplyEntityChangesAtTheDimensionBoundary() {
        Server server = new(new Properties {
            TickMode = TickMode.Region,
            SimulationDistance = 8,
            WorldProvider = "memory"
        });

        try {
            Dimension dimension = server.GetWorld().GetDimension(DimensionId.Overworld)!;
            Player player = new("Alex", "xuid", Guid.NewGuid()) {
                Position = new Vec3 { X = 8, Y = 80, Z = 8 }
            };
            player.Spawn(dimension, new EntitySpawnOptions());

            Entity source = new("test:source") {
                Position = new Vec3 { X = 8, Y = 80, Z = 8 }
            };
            source.AddTrait(new SpawnAndDespawnTrait(source));
            source.Spawn(dimension, new EntitySpawnOptions());

            Entity otherRegion = new("test:other") {
                Position = new Vec3 { X = 136, Y = 80, Z = 8 }
            };
            otherRegion.Spawn(dimension, new EntitySpawnOptions());

            server.Tick();

            Entity[] entities = dimension.GetEntitiesSnapshot();
            Entity child = Assert.Single(entities, entity => entity.Identifier == "test:child");
            Assert.DoesNotContain(source, entities);
            Assert.Contains(child, dimension.GetEntities(0, 0));
        }
        finally {
            server.Stop();
        }
    }

    [Fact]
    public void DimensionMailboxDropsCommandAfterPlayerTransfer() {
        using EmptyProvider provider = new();
        using Dimension source = new("source", DimensionId.Overworld, provider, new VoidGenerator());
        using Dimension target = new("target", DimensionId.Nether, provider, new VoidGenerator());
        Player player = new("Alex", "xuid", Guid.NewGuid());
        player.Spawn(source, new EntitySpawnOptions());
        int value = 0;

        Assert.True(source.TryEnqueue(player, () => value = 1));
        player.Teleport(new Vec3 { X = 1, Y = 80, Z = 1 }, target);

        source.Tick(1, 1);

        Assert.Equal(0, value);
        Assert.Same(target, player.Dimension);
    }

    [Fact]
    public void PlayerVisibilitySnapshotUsesNearbyChunks() {
        using EmptyProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());
        Player nearby = new("Nearby", "xuid-nearby", Guid.NewGuid()) {
            Position = new Vec3 { X = 8, Y = 80, Z = 8 }
        };
        Player distant = new("Distant", "xuid-distant", Guid.NewGuid()) {
            Position = new Vec3 { X = 1000, Y = 80, Z = 1000 }
        };

        nearby.Spawn(dimension, new EntitySpawnOptions());
        distant.Spawn(dimension, new EntitySpawnOptions());

        Player[] players = dimension.GetPlayersNearSnapshot(new Vec3 { X = 8, Y = 80, Z = 8 });

        Assert.Contains(nearby, players);
        Assert.DoesNotContain(distant, players);
    }

    [Fact]
    public void EntitySnapshotDoesNotChangeAfterOwnerMutation() {
        using EmptyProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());
        Entity entity = new("test:entity");
        entity.Spawn(dimension, new EntitySpawnOptions());

        Entity[] snapshot = dimension.GetEntitiesSnapshot();
        dimension.RemoveEntity(entity);

        Assert.Contains(entity, snapshot);
        Assert.DoesNotContain(entity, dimension.Entities);
    }

    [Fact]
    public void PlayerSnapshotDoesNotChangeAfterOwnerMutation() {
        using EmptyProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());
        Player player = new("Alex", "xuid", Guid.NewGuid());
        player.Spawn(dimension, new EntitySpawnOptions());

        Player[] snapshot = dimension.GetPlayersSnapshot();
        dimension.RemoveEntity(player);

        Assert.Contains(player, snapshot);
        Assert.DoesNotContain(player, dimension.GetPlayers());
    }

    [Fact]
    public void PublicPlayerViewUsesThePublishedSnapshot() {
        using EmptyProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());
        Player player = new("Alex", "xuid", Guid.NewGuid());
        player.Spawn(dimension, new EntitySpawnOptions());

        IReadOnlyCollection<Player> view = dimension.GetPlayers();
        dimension.RemoveEntity(player);

        Assert.Contains(player, view);
        Assert.DoesNotContain(player, dimension.GetPlayers());
        Assert.Equal(0, dimension.ActivePlayerCount);
        Assert.Equal(0, dimension.ActiveEntityCount);
    }

    [Fact]
    public void PublicEntityViewUsesThePublishedSnapshot() {
        using EmptyProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());
        Entity entity = new("test:entity");
        entity.Spawn(dimension, new EntitySpawnOptions());

        Entity[] view = dimension.GetEntitiesSnapshot();
        dimension.RemoveEntity(entity);

        Assert.Contains(entity, view);
        Assert.DoesNotContain(entity, dimension.GetEntitiesSnapshot());
    }

    [Fact]
    public void RegionSnapshotUsesChunkIndexes() {
        using EmptyProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());
        Entity near = new("test:near") {
            Position = new Vec3 { X = 8, Y = 80, Z = 8 }
        };
        Entity far = new("test:far") {
            Position = new Vec3 { X = 128, Y = 80, Z = 8 }
        };
        Player player = new("Alex", "xuid", Guid.NewGuid()) {
            Position = new Vec3 { X = 8, Y = 80, Z = 8 }
        };
        near.Spawn(dimension, new EntitySpawnOptions());
        far.Spawn(dimension, new EntitySpawnOptions());
        player.Spawn(dimension, new EntitySpawnOptions());

        RegionCoordinate region = Dimension.GetRegionCoordinate(0, 0);
        Assert.Contains(near, dimension.GetEntitiesInRegionSnapshot(region));
        Assert.Contains(player, dimension.GetPlayersInRegionSnapshot(region));
        Assert.DoesNotContain(far, dimension.GetEntitiesInRegionSnapshot(region));
        Assert.Equal(new RegionCoordinate(-1, -1), Dimension.GetRegionCoordinate(-1, -1));
    }

    [Fact]
    public void RegionWriteDropsAfterEntityMoves() {
        using EmptyProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());
        Entity entity = new("test:entity") {
            Position = new Vec3 { X = 8, Y = 80, Z = 8 }
        };
        entity.Spawn(dimension, new EntitySpawnOptions());
        int writes = 0;

        Assert.True(dimension.TryEnqueueRegion(
            entity,
            Dimension.GetRegionCoordinate(0, 0),
            () => writes++));
        entity.Position = new Vec3 { X = 128, Y = 80, Z = 8 };
        dimension.Tick(1, 1);

        Assert.Equal(0, writes);
    }

    [Fact]
    public void ContiguousChunkBucketKeepsIndexesAfterRemoval() {
        using EmptyProvider provider = new();
        using Dimension dimension = new("test", DimensionId.Overworld, provider, new VoidGenerator());
        Entity first = new("test:first");
        Entity second = new("test:second");
        Entity third = new("test:third");
        first.Spawn(dimension, new EntitySpawnOptions());
        second.Spawn(dimension, new EntitySpawnOptions());
        third.Spawn(dimension, new EntitySpawnOptions());

        dimension.RemoveEntity(second);

        Entity[] entities = dimension.GetEntities(0, 0).ToArray();
        Assert.Equal(2, entities.Length);
        Assert.Contains(first, entities);
        Assert.Contains(third, entities);
        Assert.DoesNotContain(second, entities);
    }

    [Fact]
    public void EntityKeepsOneOwnerDuringItsTick() {
        Server server = new(new Properties { WorldProvider = "memory" });
        try {
            Dimension dimension = server.GetWorld().GetDimension(DimensionId.Overworld)!;
            Player player = new("Owner", "xuid", Guid.NewGuid()) {
                Position = new Vec3 { X = 8, Y = 80, Z = 8 }
            };
            player.Spawn(dimension, new EntitySpawnOptions());
            Entity entity = new("test:owner") {
                Position = new Vec3 { X = 8, Y = 80, Z = 8 }
            };
            OwnerObservationTrait trait = new(entity);
            entity.AddTrait(trait);
            entity.Spawn(dimension, new EntitySpawnOptions());

            server.Tick();

            Assert.True(trait.OwnerHeld);
            Assert.False(entity.TickOwnedBy(dimension));
        }
        finally {
            server.Stop();
        }
    }

    private sealed class EmptyProvider : WorldProvider {
        public override string Identifier => "empty";

        public override bool HasChunk(DimensionId dimensionType, int x, int z) => false;

        public override Chunk? LoadChunk(DimensionId dimensionType, int x, int z) => null;

        public override void SaveChunk(Chunk chunk) { }

        public override void DeleteChunk(DimensionId dimensionType, int x, int z) { }

        public override void Dispose() { }
    }

    private sealed class SpawnAndDespawnTrait : EntityTrait {
        private bool _ran;

        public new static string Identifier => "test:spawn_and_despawn";

        public SpawnAndDespawnTrait(Entity entity) : base(entity) { }

        public override void OnTick(TraitOnTickDetails details) {
            if (_ran) {
                return;
            }

            _ran = true;
            Entity child = new("test:child") {
                Position = Entity.Position
            };
            child.Spawn(Dimension, new EntitySpawnOptions());
            Entity.Despawn(new EntityDespawnOptions());
        }

        public override EntityTrait Clone(Entity entity) => new SpawnAndDespawnTrait(entity);
    }

    private sealed class OwnerObservationTrait : EntityTrait {
        public bool OwnerHeld { get; private set; }

        public OwnerObservationTrait(Entity entity) : base(entity) { }

        public override void OnTick(TraitOnTickDetails details) {
            OwnerHeld = Entity.Dimension is { } dimension && Entity.TickOwnedBy(dimension);
        }

        public override EntityTrait Clone(Entity entity) => new OwnerObservationTrait(entity);
    }
}
