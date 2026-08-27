namespace Basalt.Tests;

using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;
using Basalt.Core;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Events;
using Basalt.Core.Network;
using Basalt.Core.Player;
using Basalt.Core.Worlds.Dimensions;

[Collection("Server tests")]
public sealed class PlayerChatTests {
    [Fact]
    public void PlayerChatRunsInThePlayerDimension() {
        string worldPath = Path.Combine(Path.GetTempPath(), $"basalt-chat-{Guid.NewGuid():N}");
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

        bool chatRan = false;
        server.On<PlayerChatSignal>(ServerEvent.PlayerChat, _ => chatRan = true);

        NetworkConnection connection = new((_, _, _) => { }, () => { });
        server.Players[connection] = player;
        TextPacket packet = new() {
            Body = new TextPacketBody { Message = "hello" }
        };

        Basalt.Core.Network.Handlers.Text.Handle(server, connection, packet);

        Assert.False(chatRan);
        dimension.Tick(1, 1);
        Assert.True(chatRan);
    }
}
