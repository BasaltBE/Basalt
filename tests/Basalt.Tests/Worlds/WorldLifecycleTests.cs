namespace Basalt.Tests;

using Basalt.Core;
using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Worlds.Dimensions.Generation;

[Collection("Server tests")]
public sealed class WorldLifecycleTests {
    [Fact]
    public void WorldRemovalWaitsForTheServerTickBoundary() {
        string worldPath = Path.Combine(Path.GetTempPath(), $"basalt-unload-{Guid.NewGuid():N}");
        Properties properties = new() {
            WorldProvider = "memory",
            WorldPath = worldPath,
            PlayerDataPath = Path.Combine(worldPath, "players"),
            PluginsDirectory = Path.Combine(worldPath, "plugins"),
            ResourcePacksPath = Path.Combine(worldPath, "resource-packs"),
            RconPort = 0
        };

        using Support.TestServerLifetime serverLifetime = new(properties);
        Server server = serverLifetime.Server;
        World extra = server.CreateWorld("extra", "memory");
        extra.CreateDimension("overworld", DimensionId.Overworld, typeof(VoidGenerator));
        Dimension dimension = server.GetWorld().GetDimension(DimensionId.Overworld)!;

        Assert.True(dimension.TryEnqueue(() => Assert.True(server.UnloadWorld("extra"))));

        server.Tick();

        Assert.Throws<KeyNotFoundException>(() => server.GetWorld("extra"));
    }
}
