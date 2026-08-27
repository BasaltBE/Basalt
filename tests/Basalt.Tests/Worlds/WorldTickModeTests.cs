namespace Basalt.Tests;

using Basalt.Core;
using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Player;
using Basalt.Core.Tasks;
using Basalt.Core.Enums;
using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Worlds.Dimensions.Chunk;
using Basalt.Core.Worlds.Dimensions.Generation;
using Basalt.Core.Worlds.Dimensions.Provider;

public sealed class WorldTickModeTests {
    [Fact]
    public void ExistingTickModeValuesRemainStable() {
        Assert.Equal(0, (int)TickMode.Single);
        Assert.Equal(1, (int)TickMode.World);
        Assert.Equal(2, (int)TickMode.Dimension);
        Assert.Equal(3, (int)TickMode.Region);
        Assert.Equal(4, (int)TickMode.Adaptive);
    }

    [Fact]
    public void TickGroupChangesApplyAtTheServerBoundary() {
        Server server = new(new Properties {
            TickMode = TickMode.Group,
            TickGroups = 1,
            WorldProvider = "memory"
        });

        try {
            server.RequestTickGroups(2);

            Assert.Equal(1, server.Properties.TickGroups);
            server.Tick();

            Assert.Equal(2, server.Properties.TickGroups);
        }
        finally {
            server.Stop();
        }
    }

    [Fact]
    public void WorldModeTicksAllWorlds() {
        Server server = new(new Properties {
            TickMode = TickMode.World,
            WorldProvider = "memory"
        });
        try {
            World first = server.CreateWorld("first", "memory");
            World second = server.CreateWorld("second", "memory");
            first.CreateDimension("overworld", DimensionId.Overworld, typeof(VoidGenerator));
            second.CreateDimension("overworld", DimensionId.Overworld, typeof(VoidGenerator));

            server.Tick();

            Assert.Equal(1UL, first.TickValue);
            Assert.Equal(1UL, second.TickValue);
        }
        finally {
            server.Stop();
        }
    }

    [Fact]
    public void GroupModeTicksAllWorlds() {
        Server server = new(new Properties {
            TickMode = TickMode.Group,
            TickGroups = 2,
            WorldProvider = "memory"
        });

        try {
            World first = server.CreateWorld("first", "memory");
            World second = server.CreateWorld("second", "memory");
            World third = server.CreateWorld("third", "memory");
            first.CreateDimension("overworld", DimensionId.Overworld, typeof(VoidGenerator));
            second.CreateDimension("overworld", DimensionId.Overworld, typeof(VoidGenerator));
            third.CreateDimension("overworld", DimensionId.Overworld, typeof(VoidGenerator));

            server.Tick();

            Assert.Equal(1UL, first.TickValue);
            Assert.Equal(1UL, second.TickValue);
            Assert.Equal(1UL, third.TickValue);
        }
        finally {
            server.Stop();
        }
    }

    [Fact]
    public void AdaptiveModeTicksSafeDimensionsInParallel() {
        Server server = new(new Properties {
            TickMode = TickMode.Adaptive,
            WorldProvider = "memory"
        });

        try {
            World world = server.GetWorld();
            Dimension nether = world.CreateDimension("nether", DimensionId.Nether, typeof(VoidGenerator));
            int firstValue = 0;
            int secondValue = 0;
            Dimension overworld = world.GetDimension(DimensionId.Overworld)!;

            Assert.True(overworld.TryEnqueue(() => firstValue = 1));
            Assert.True(nether.TryEnqueue(() => secondValue = 1));
            server.Tick();

            Assert.Equal(1, firstValue);
            Assert.Equal(1, secondValue);
        }
        finally {
            server.Stop();
        }
    }

    [Fact]
    public void AdaptiveModeTicksIndependentWorlds() {
        Server server = new(new Properties {
            TickMode = TickMode.Adaptive,
            WorkerThreads = 2,
            WorldProvider = "memory"
        });

        try {
            World first = server.CreateWorld("first", "memory");
            World second = server.CreateWorld("second", "memory");
            first.CreateDimension("overworld", DimensionId.Overworld, typeof(VoidGenerator));
            second.CreateDimension("overworld", DimensionId.Overworld, typeof(VoidGenerator));

            server.Tick();

            Assert.Equal(1UL, first.TickValue);
            Assert.Equal(1UL, second.TickValue);
        }
        finally {
            server.Stop();
        }
    }

    [Fact]
    public void AdaptiveModeDoesNotNestRegionJobsAcrossWorlds() {
        Server server = new(new Properties {
            TickMode = TickMode.Adaptive,
            WorkerThreads = 2,
            SimulationDistance = 8,
            WorldProvider = "memory"
        });

        try {
            World firstWorld = server.CreateWorld("first", "memory");
            World secondWorld = server.CreateWorld("second", "memory");
            Dimension firstDimension = firstWorld.CreateDimension("overworld", DimensionId.Overworld, typeof(VoidGenerator));
            Dimension secondDimension = secondWorld.CreateDimension("overworld", DimensionId.Overworld, typeof(VoidGenerator));
            OwnerObservationTrait firstTrait = AddObservedEntity(firstDimension, "first");
            OwnerObservationTrait secondTrait = AddObservedEntity(secondDimension, "second");

            server.Tick();

            Assert.True(firstTrait.OwnerHeld);
            Assert.True(secondTrait.OwnerHeld);
            Assert.NotEqual(Environment.CurrentManagedThreadId, firstTrait.ThreadId);
            Assert.NotEqual(Environment.CurrentManagedThreadId, secondTrait.ThreadId);
        }
        finally {
            server.Stop();
        }
    }

    [Fact]
    public void AdaptiveModeSplitsHotWorldsAndMergesQuietWorlds() {
        Server server = new(new Properties {
            TickMode = TickMode.Adaptive,
            WorkerThreads = 2,
            WorldProvider = "memory"
        });

        try {
            World extra = server.CreateWorld("extra", "memory");
            extra.CreateDimension("overworld", DimensionId.Overworld, typeof(VoidGenerator));
            server.GetWorld().TickWork = 10;

            server.Tick();
            Assert.Equal(2, server.LastAdaptiveGroupCount);

            server.GetWorld().TickWork = 0;
            extra.TickWork = 0;
            server.Tick();
            Assert.Equal(1, server.LastAdaptiveGroupCount);
        }
        finally {
            server.Stop();
        }
    }

    [Fact]
    public void AdaptiveModeTicksActiveRegionsInOneWorld() {
        Server server = new(new Properties {
            TickMode = TickMode.Adaptive,
            WorkerThreads = 2,
            SimulationDistance = 8,
            WorldProvider = "memory"
        });

        try {
            Dimension dimension = server.GetWorld().GetDimension(DimensionId.Overworld)!;
            Player player = new("Adaptive", "adaptive", Guid.NewGuid()) {
                Position = new Basalt.BedrockProtocol.Types.Vec3 { X = 8, Y = 80, Z = 8 }
            };
            player.Spawn(dimension, new EntitySpawnOptions());

            Entity first = new("test:adaptive_first") {
                Position = new Basalt.BedrockProtocol.Types.Vec3 { X = 8, Y = 80, Z = 8 }
            };
            Entity second = new("test:adaptive_second") {
                Position = new Basalt.BedrockProtocol.Types.Vec3 { X = 136, Y = 80, Z = 8 }
            };
            OwnerObservationTrait firstTrait = new(first);
            OwnerObservationTrait secondTrait = new(second);
            first.AddTrait(firstTrait);
            second.AddTrait(secondTrait);
            first.Spawn(dimension, new EntitySpawnOptions());
            second.Spawn(dimension, new EntitySpawnOptions());

            server.Tick();

            Assert.True(firstTrait.OwnerHeld);
            Assert.True(secondTrait.OwnerHeld);
            Assert.NotEqual(Environment.CurrentManagedThreadId, firstTrait.ThreadId);
            Assert.NotEqual(Environment.CurrentManagedThreadId, secondTrait.ThreadId);
        }
        finally {
            server.Stop();
        }
    }

    [Fact]
    public void RegionModeTicksEntitiesInSpatialPartitions() {
        Server server = new(new Properties {
            TickMode = TickMode.Region,
            SimulationDistance = 8,
            WorldProvider = "memory"
        });

        try {
            Dimension dimension = server.GetWorld().GetDimension(DimensionId.Overworld)!;
            Player player = new("Region", "region", Guid.NewGuid()) {
                Position = new Basalt.BedrockProtocol.Types.Vec3 { X = 8, Y = 80, Z = 8 }
            };
            player.Spawn(dimension, new EntitySpawnOptions());

            Entity first = new("test:first") {
                Position = new Basalt.BedrockProtocol.Types.Vec3 { X = 8, Y = 80, Z = 8 }
            };
            Entity second = new("test:second") {
                Position = new Basalt.BedrockProtocol.Types.Vec3 { X = 136, Y = 80, Z = 8 }
            };
            OwnerObservationTrait firstTrait = new(first);
            OwnerObservationTrait secondTrait = new(second);
            first.AddTrait(firstTrait);
            second.AddTrait(secondTrait);
            first.Spawn(dimension, new EntitySpawnOptions());
            second.Spawn(dimension, new EntitySpawnOptions());

            server.Tick();

            Assert.True(firstTrait.OwnerHeld);
            Assert.True(secondTrait.OwnerHeld);
            Assert.NotEqual(Environment.CurrentManagedThreadId, firstTrait.ThreadId);
            Assert.NotEqual(Environment.CurrentManagedThreadId, secondTrait.ThreadId);
        }
        finally {
            server.Stop();
        }
    }

    [Fact]
    public void RegionChunkSizeComesFromServerProperties() {
        Server server = new(new Properties {
            TickMode = TickMode.Region,
            RegionChunkSize = 4,
            WorldProvider = "memory"
        });

        try {
            Dimension dimension = server.GetWorld().GetDimension(DimensionId.Overworld)!;

            Assert.Equal(4, dimension.RegionChunkSize);
            Assert.Equal(new RegionCoordinate(1, 0), Dimension.GetRegionCoordinate(4, 0, 4));
        }
        finally {
            server.Stop();
        }
    }

    private sealed class OwnerObservationTrait : Basalt.Core.Entities.Traits.EntityTrait {
        public bool OwnerHeld { get; private set; }
        public int ThreadId { get; private set; }

        public OwnerObservationTrait(Entity entity) : base(entity) { }

        public override void OnTick(Basalt.Core.Traits.TraitOnTickDetails details) {
            ThreadId = Environment.CurrentManagedThreadId;
            OwnerHeld = Entity.Dimension is { } dimension && Entity.TickOwnedBy(dimension);
        }

        public override Basalt.Core.Entities.Traits.EntityTrait Clone(Entity entity) => new OwnerObservationTrait(entity);
    }

    private static OwnerObservationTrait AddObservedEntity(Dimension dimension, string name) {
        Player player = new(name, name, Guid.NewGuid()) {
            Position = new Basalt.BedrockProtocol.Types.Vec3 { X = 8, Y = 80, Z = 8 }
        };
        player.Spawn(dimension, new EntitySpawnOptions());

        Entity entity = new($"test:{name}") {
            Position = new Basalt.BedrockProtocol.Types.Vec3 { X = 136, Y = 80, Z = 8 }
        };
        OwnerObservationTrait trait = new(entity);
        entity.AddTrait(trait);
        entity.Spawn(dimension, new EntitySpawnOptions());
        return trait;
    }

    [Fact]
    public void ParallelDimensionCannotCreateWorldOffCoordinator() {
        Server server = new(new Properties {
            TickMode = TickMode.Adaptive,
            WorldProvider = "memory"
        });

        try {
            World world = server.GetWorld();
            Dimension nether = world.CreateDimension("nether", DimensionId.Nether, typeof(VoidGenerator));
            bool rejected = false;

            Assert.True(nether.TryEnqueue(() => {
                try {
                    server.CreateWorld("unsafe", "memory");
                }
                catch (InvalidOperationException) {
                    rejected = true;
                }
            }));

            server.Tick();

            Assert.True(rejected);
            Assert.Throws<KeyNotFoundException>(() => server.GetWorld("unsafe"));
        }
        finally {
            server.Stop();
        }
    }

    [Fact]
    public void ParallelTickDrainsAllDimensionMailboxes() {
        using World world = new("test", new EmptyProvider());
        Dimension first = new("first", DimensionId.Overworld, world.Provider, new VoidGenerator());
        Dimension second = new("second", DimensionId.Nether, world.Provider, new VoidGenerator());
        world.AddDimension(first);
        world.AddDimension(second);

        int firstValue = 0;
        int secondValue = 0;
        Assert.True(first.TryEnqueue(() => firstValue = 1));
        Assert.True(second.TryEnqueue(() => secondValue = 1));

        using TaskWorkerPool workerPool = new(2);
        world.TickDimensionsParallel(workerPool);

        Assert.Equal(1, firstValue);
        Assert.Equal(1, secondValue);
        Assert.True(first.TickWork >= 0);
        Assert.True(second.TickWork >= 0);
    }

    [Fact]
    public void DimensionRemovalWaitsForTheWorldTickBoundary() {
        using World world = new("test", new EmptyProvider());
        Dimension first = new("first", DimensionId.Overworld, world.Provider, new VoidGenerator());
        Dimension second = new("second", DimensionId.Nether, world.Provider, new VoidGenerator());
        world.AddDimension(first);
        world.AddDimension(second);

        Assert.True(first.TryEnqueue(() => Assert.True(world.RemoveDimension("second"))));

        world.Tick();

        Assert.Null(world.GetDimension("second"));
    }

    private sealed class EmptyProvider : WorldProvider {
        public override string Identifier => "empty";

        public override bool HasChunk(DimensionId dimensionType, int x, int z) => false;

        public override Chunk? LoadChunk(DimensionId dimensionType, int x, int z) => null;

        public override void SaveChunk(Chunk chunk) { }

        public override void DeleteChunk(DimensionId dimensionType, int x, int z) { }

        public override void Dispose() { }
    }
}
