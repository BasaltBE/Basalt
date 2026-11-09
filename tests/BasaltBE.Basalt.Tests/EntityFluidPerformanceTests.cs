using System.Diagnostics;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using Xunit;
using Xunit.Abstractions;

namespace BasaltBE.Basalt.Tests;

public sealed class EntityFluidPerformanceTests(ITestOutputHelper output) {
    [Theory]
    [InlineData("minecraft:air", false)]
    [InlineData("minecraft:water", false)]
    [InlineData("minecraft:lava", false)]
    [InlineData("minecraft:air", true)]
    [InlineData("minecraft:water", true)]
    [InlineData("minecraft:lava", true)]
    public void WarmMovementHasNoPerTickAllocations(string medium, bool movingBounds) {
        using MultiplayerFixture fixture = new();
        BlockPermutation block = BlockPalette.FindDefaultPermutation(medium)!;
        for (int x = 0; x < 3; x++) {
            for (int z = 0; z < 3; z++) {
                for (int y = 0; y < 4; y++)
                    fixture.Dimension.SetPermutation(x, y, z, block);
            }
        }
        Entity[] entities = new Entity[1000];
        EntityMovementTrait[] movements = new EntityMovementTrait[entities.Length];
        for (int i = 0; i < entities.Length; i++) {
            Entity entity = new("minecraft:cow");
            EntityMovementTrait movement = new(entity);
            entity.AddTrait(movement);
            entity.Position.X = entity.Position.Z = 1.5f;
            entity.Position.Y = 1;
            entity.Spawn(fixture.Dimension);
            entities[i] = entity;
            movements[i] = movement;
        }
        double[] milliseconds = new double[100];
        long bytes = 0;
        for (int tick = 0; tick < 300; tick++) {
            long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < entities.Length; i++) {
                Entity entity = entities[i];
                EntityMovementTrait movement = movements[i];
                entity.Position.X = 1.5f + (movingBounds ? tick % 8 * 0.1f : 0);
                entity.Position.Z = 1.5f;
                entity.Position.Y = 1;
                entity.SimulationAge++;
                movement.Motion.X = 0.1f;
                movement.Motion.Y = movement.Motion.Z = 0;
                movement.Tick();
            }
            if (tick >= 200) {
                milliseconds[tick - 200] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                bytes += GC.GetAllocatedBytesForCurrentThread() - beforeBytes;
            }
        }
        Array.Sort(milliseconds);
        output.WriteLine($"{medium} (moving bounds: {movingBounds}): 1000 entities, median: {milliseconds[50]:F3} ms, p95: {milliseconds[95]:F3} ms, allocation: {bytes} bytes / 100 ticks.");
        Assert.Equal(0, bytes);
    }
}
