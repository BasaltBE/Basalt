namespace Basalt.Tests;

using Basalt.Core.Network;

public sealed class NetworkConnectionTests {
    [Fact]
    public void UnreliablePacketsUseTheUnreliableTransport() {
        int reliableSends = 0;
        int unreliableSends = 0;
        NetworkConnection connection = new(
            (_, _, _) => reliableSends++,
            () => { },
            unreliableSend: (_, _, _) => unreliableSends++);

        connection.SendPacket([1], unreliable: false);
        connection.SendPacket([2], unreliable: true);

        Assert.Equal(1, reliableSends);
        Assert.Equal(1, unreliableSends);
    }
}
