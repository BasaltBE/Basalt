using System.Diagnostics;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.BlockActors;
using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Basalt.Worlds.Types;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Types;
using Xunit;
using Xunit.Abstractions;

namespace BasaltBE.Basalt.Tests;

public sealed class HopperTests(ITestOutputHelper output) {
    [Fact]
    public void PushesOneItemAndPreservesStackData() {
        using Dimension dimension = new("test:hopper-push", new VoidGenerator());
        (HopperBlockActor hopper, Chunk chunk) = CreateHopper(dimension, facing: 5);
        BarrelBlockActor barrel = CreateBarrel(dimension, chunk, 9, 100, 8);
        CompoundTag itemData = new();
        itemData.Add("CustomName", new StringTag { Value = "Tagged iron" });
        hopper.Container.SetItem(0, new ItemStack("minecraft:iron_ingot", 2, 3, itemData));

        hopper.Tick();

        Assert.Equal((ushort)1, hopper.Container.GetItem(0)!.StackSize);
        Assert.Equal((ushort)1, barrel.Container.GetItem(0)!.StackSize);
        Assert.Equal(3U, barrel.Container.GetItem(0)!.Metadata);
        Assert.Equal("Tagged iron", barrel.Container.GetItem(0)!.Nbt!.Get<StringTag>("CustomName")!.Value);
        Assert.Equal(8, hopper.TransferCooldown);
    }

    [Fact]
    public void PullsFromAContainerAboveAndCollectsLooseItems() {
        using Dimension dimension = new("test:hopper-pull", new VoidGenerator());
        (HopperBlockActor hopper, Chunk chunk) = CreateHopper(dimension);
        BarrelBlockActor barrel = CreateBarrel(dimension, chunk, 8, 101, 8);
        barrel.Container.SetItem(0, new ItemStack("minecraft:coal", 2));

        hopper.Tick();

        Assert.Equal((ushort)1, hopper.Container.GetItem(0)!.StackSize);
        Assert.Equal((ushort)1, barrel.Container.GetItem(0)!.StackSize);

        hopper.TransferCooldown = 0;
        hopper.Container.Clear();
        barrel.Container.Clear();
        ItemEntity dropped = new(new ItemStack("minecraft:stick", 1), 8, 101, 8);
        dropped.Spawn(dimension);

        hopper.Tick();

        Assert.Equal("minecraft:stick", hopper.Container.GetItem(0)!.Type.Identifier);
        Assert.Null(dropped.SpawnedDimension);
    }

    [Fact]
    public void PoweredHopperStopsTransfersAndResumesWhenPowerIsRemoved() {
        using Dimension dimension = new("test:hopper-lock", new VoidGenerator());
        (HopperBlockActor hopper, Chunk chunk) = CreateHopper(dimension, facing: 5);
        BarrelBlockActor barrel = CreateBarrel(dimension, chunk, 9, 100, 8);
        hopper.Container.SetItem(0, new ItemStack("minecraft:iron_ingot", 1));
        BlockPermutation lever = BlockPalette.FindDefaultPermutation("minecraft:lever")!;
        BlockPermutation poweredLever = BlockPalette.Find(lever.Identifier, lever.State, "open_bit", true)!;
        dimension.SetPermutation(8, 100, 7, poweredLever);

        hopper.Tick();

        Assert.Null(barrel.Container.GetItem(0));
        Assert.True(dimension.GetPermutation(8, 100, 8).State.Values.GetValueOrDefault("toggle_bit") is true);

        dimension.SetPermutation(8, 100, 7, lever);
        hopper.TransferCooldown = 0;
        hopper.Tick();

        Assert.Equal("minecraft:iron_ingot", barrel.Container.GetItem(0)!.Type.Identifier);
        Assert.False(dimension.GetPermutation(8, 100, 8).State.Values.GetValueOrDefault("toggle_bit") is true);
    }

    [Fact]
    public void SavesContentsAndCooldownInHopperNbt() {
        using Dimension dimension = new("test:hopper-nbt", new VoidGenerator());
        (HopperBlockActor hopper, _) = CreateHopper(dimension);
        hopper.Container.SetItem(2, new ItemStack("minecraft:diamond", 4, 7));
        hopper.TransferCooldown = 5;
        CompoundTag nbt = new();

        hopper.OnWrite(nbt);

        HopperBlockActor restored = new(new BlockActorContext(
            dimension,
            new BlockPos { X = 8, Y = 100, Z = 8 },
            "minecraft:hopper",
            hopper.Permutation,
            nbt
        ));
        restored.OnRead(nbt);

        Assert.Equal(5, restored.TransferCooldown);
        Assert.Equal("minecraft:diamond", restored.Container.GetItem(2)!.Type.Identifier);
        Assert.Equal((ushort)4, restored.Container.GetItem(2)!.StackSize);
        Assert.Equal(7U, restored.Container.GetItem(2)!.Metadata);
    }

    [Fact]
    public void BenchmarkActiveHopperTicks() {
        const int warmupCount = 30;
        const int sampleCount = 200;
        foreach (int count in new[] { 1, 100, 1000 }) {
            foreach (string scenario in new[] { "cooldown", "powered", "full destination", "transfer" }) {
                bool facesBarrel = scenario is "full destination" or "transfer";
                int facing = facesBarrel ? 5 : 0;
                using Dimension dimension = new($"test:hopper-benchmark-{count}-{scenario}", new VoidGenerator());
                BlockPermutation hopperType = BlockPalette.FindDefaultPermutation("minecraft:hopper")!;
                BlockPermutation hopperPermutation = BlockPalette.Find(
                    hopperType.Identifier,
                    hopperType.State,
                    "facing_direction",
                    facing
                )!;
                BlockPermutation barrelPermutation = BlockPalette.FindDefaultPermutation("minecraft:barrel")!;
                BlockPermutation lever = BlockPalette.FindDefaultPermutation("minecraft:lever")!;
                BlockPermutation poweredLever = BlockPalette.Find(lever.Identifier, lever.State, "open_bit", true)!;
                List<HopperBlockActor> hoppers = new(count);
                List<BarrelBlockActor> barrels = new(count);
                for (int index = 0; index < count; index++) {
                    int x = index * 2 + 2;
                    Chunk chunk = dimension.GetOrCreateChunk(x >> 4, 0);
                    dimension.SetPermutation(x, 100, 8, hopperPermutation);
                    HopperBlockActor hopper = new(new BlockActorContext(
                        dimension,
                        new BlockPos { X = x, Y = 100, Z = 8 },
                        "minecraft:hopper",
                        hopperPermutation,
                        new CompoundTag()
                    ));
                    chunk.SetRuntimeActor(x & 15, 100, 8, hopper);
                    hoppers.Add(hopper);

                    int targetX = x + 1;
                    dimension.SetPermutation(targetX, 100, 8, barrelPermutation);
                    BarrelBlockActor barrel = new(new BlockActorContext(
                        dimension,
                        new BlockPos { X = targetX, Y = 100, Z = 8 },
                        "minecraft:barrel",
                        barrelPermutation,
                        new CompoundTag()
                    ));
                    chunk.SetRuntimeActor(targetX & 15, 100, 8, barrel);
                    barrels.Add(barrel);

                    if (scenario == "powered")
                        dimension.SetPermutation(x, 100, 7, poweredLever);
                    else if (scenario == "full destination") {
                        for (int slot = 0; slot < barrel.Container.ContainerSize; slot++)
                            barrel.Container.SetItem(slot, new ItemStack("minecraft:stone", 64));
                        hopper.Container.SetItem(0, new ItemStack("minecraft:iron_ingot", 1));
                    }
                }

                if (scenario == "powered") {
                    foreach (HopperBlockActor hopper in hoppers)
                        hopper.Tick();
                }

                double[] samples = new double[sampleCount];
                long allocated = 0;
                for (int sample = -warmupCount; sample < sampleCount; sample++) {
                    foreach (HopperBlockActor hopper in hoppers) {
                        hopper.TransferCooldown = scenario is "cooldown" or "powered" ? 4 : 0;
                        if (scenario == "transfer")
                            hopper.Container.SetItem(0, new ItemStack("minecraft:iron_ingot", 1));
                    }
                    if (scenario == "transfer") {
                        foreach (BarrelBlockActor barrel in barrels)
                            barrel.Container.Clear();
                    }

                    long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                    long started = Stopwatch.GetTimestamp();
                    foreach (HopperBlockActor hopper in hoppers)
                        hopper.Tick();
                    double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                    long sampleAllocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
                    if (sample >= 0) {
                        samples[sample] = elapsed;
                        allocated += sampleAllocated;
                    }
                }

                Array.Sort(samples);
                output.WriteLine(
                    $"Hopper {scenario}, {count} actors, {sampleCount} samples, total p50 {Percentile(samples, 0.50):F4} ms, p95 {Percentile(samples, 0.95):F4} ms, p99 {Percentile(samples, 0.99):F4} ms, max {samples[^1]:F4} ms, {allocated / (double)sampleCount:F0} bytes per batch."
                );
            }
        }
    }

    static (HopperBlockActor Actor, Chunk Chunk) CreateHopper(Dimension dimension, int facing = 5) {
        BlockPermutation hopper = BlockPalette.Find(
            "minecraft:hopper",
            BlockPalette.FindDefaultPermutation("minecraft:hopper")!.State,
            "facing_direction",
            facing
        )!;
        Chunk chunk = dimension.GetOrCreateChunk(0, 0);
        dimension.SetPermutation(8, 100, 8, hopper);
        HopperBlockActor actor = new(new BlockActorContext(
            dimension,
            new BlockPos { X = 8, Y = 100, Z = 8 },
            "minecraft:hopper",
            hopper,
            new CompoundTag()
        ));
        chunk.SetRuntimeActor(8, 100, 8, actor);
        return (actor, chunk);
    }

    static BarrelBlockActor CreateBarrel(Dimension dimension, Chunk chunk, int x, int y, int z) {
        BlockPermutation barrel = BlockPalette.FindDefaultPermutation("minecraft:barrel")!;
        dimension.SetPermutation(x, y, z, barrel);
        BarrelBlockActor actor = new(new BlockActorContext(
            dimension,
            new BlockPos { X = x, Y = y, Z = z },
            "minecraft:barrel",
            barrel,
            new CompoundTag()
        ));
        chunk.SetRuntimeActor(x & 15, y, z & 15, actor);
        return actor;
    }

    static double Percentile(double[] sorted, double percentile) =>
        sorted[(int)Math.Ceiling((sorted.Length - 1) * percentile)];
}
