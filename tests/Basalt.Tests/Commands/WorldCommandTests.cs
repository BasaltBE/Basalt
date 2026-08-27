namespace Basalt.Tests;

using Basalt.Core;
using Basalt.Core.Commands;
using Basalt.Core.Player;
using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Worlds.Dimensions.Generation;

[Collection("Server tests")]
public sealed class WorldCommandTests {
    [Fact]
    public void WorldsCommandListsDimensionsAndTickLoad() {
        Properties properties = new() {
            WorldProvider = "memory",
            RconPort = 0
        };

        using Support.TestServerLifetime serverLifetime = new(properties);
        Server server = serverLifetime.Server;
        World extra = server.CreateWorld("extra", "memory");
        extra.CreateDimension("nether", DimensionId.Nether, typeof(VoidGenerator));
        Player player = new("Admin", string.Empty, Guid.NewGuid());
        player.SetOperator(true, syncClient: false);

        CommandResult result = server.Commands.Execute(server, player, "/worlds");

        Assert.True(result.Success);
        Assert.Contains("extra", result.Message);
        Assert.Contains("nether", result.Message);
        Assert.Contains("ms", result.Message);
        Assert.Contains("entities", result.Message);
        Assert.Contains("chunks", result.Message);
    }
}
