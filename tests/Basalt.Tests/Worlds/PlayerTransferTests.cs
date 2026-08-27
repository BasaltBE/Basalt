namespace Basalt.Tests;

using Basalt.Core;
using Basalt.Core.Enums;
using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Player;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Worlds.Dimensions.Generation;
using Basalt.BedrockProtocol.Types;

[Collection("Server tests")]
public sealed class PlayerTransferTests {
    [Fact]
    public void CrossDimensionTeleportWaitsForTransferBoundary() {
        string worldPath = Path.Combine(Path.GetTempPath(), $"basalt-transfer-{Guid.NewGuid():N}");
        Properties properties = new() {
            WorldProvider = "memory",
            WorldPath = worldPath,
            PlayerDataPath = Path.Combine(worldPath, "players"),
            PluginsDirectory = Path.Combine(worldPath, "plugins"),
            ResourcePacksPath = Path.Combine(worldPath, "resource-packs"),
            RconPort = 0
        };

        using ServerLifetime serverLifetime = new(properties);
        Server server = serverLifetime.Server;
        Dimension source = server.GetWorld().GetDimension(DimensionId.Overworld)!;
        Dimension target = server.GetWorld().CreateDimension(
            "transfer-target",
            DimensionId.Nether,
            typeof(VoidGenerator));
        Player player = new("Test", string.Empty, Guid.NewGuid());
        player.Spawn(source, new EntitySpawnOptions(InitialSpawn: true));
        source.AddPlayer(player);

        player.Teleport(new Vec3 { X = 4, Y = 80, Z = 4 }, target);

        Assert.Same(source, player.Dimension);
        Assert.Contains(player, source.GetPlayers());
        Assert.DoesNotContain(player, target.GetPlayers());

        server.ApplyPlayerTransfers();

        Assert.Same(target, player.Dimension);
        Assert.DoesNotContain(player, source.GetPlayers());
        Assert.Contains(player, target.GetPlayers());
    }

    private sealed class ServerLifetime : IDisposable {
        public ServerLifetime(Properties properties) {
            Server = new Server(properties);
        }

        public Server Server { get; }

        public void Dispose() {
            Server.Stop();
        }
    }
}
