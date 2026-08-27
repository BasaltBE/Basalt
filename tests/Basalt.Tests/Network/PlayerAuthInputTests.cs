namespace Basalt.Tests;

using Basalt.BedrockProtocol.Types;
using Basalt.BedrockProtocol.Packets;
using Basalt.Core;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Network;
using Basalt.Core.Player;
using Basalt.Core.Worlds.Dimensions;

[Collection("Server tests")]
public sealed class PlayerAuthInputTests {
    [Fact]
    public void MovementInputsUseTheLatestQueuedPacket() {
        string worldPath = Path.Combine(Path.GetTempPath(), $"basalt-input-{Guid.NewGuid():N}");
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
        Dimension dimension = server.GetWorld().GetDimension(DimensionId.Overworld)!;
        Player player = new("Test", string.Empty, Guid.NewGuid());
        player.Spawn(dimension, new EntitySpawnOptions(InitialSpawn: true));
        NetworkConnection connection = new((_, _, _) => { }, () => { });
        server.Players[connection] = player;

        Basalt.Core.Network.Handlers.PlayerAuthInput.Handle(server, connection, new PlayerAuthInputPacket {
            Position = new Vec3 { X = 1, Y = 80, Z = 1 },
            ClientTick = 1
        });
        Basalt.Core.Network.Handlers.PlayerAuthInput.Handle(server, connection, new PlayerAuthInputPacket {
            Position = new Vec3 { X = 2, Y = 80, Z = 2 },
            ClientTick = 2
        });

        Assert.Equal(1, dimension.Mailbox.PendingCount);
        dimension.Tick(1, 1);

        Assert.Equal(2, player.Position.X);
        Assert.Equal(2, player.Position.Z);
    }
}
