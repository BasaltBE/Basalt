using BasaltBE.Basalt.Network.Events;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Packets;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class EventEmitterTests {
    [Fact]
    public void PacketSubscribersBelongToTheirEmitter() {
        EventEmitter first = new();
        EventEmitter second = new();
        int received = 0;
        first.Subscribe(PacketEvents.ClientCacheStatus, (_, _, _) => received++);

        Assert.True(first.Emit(129, new NethernetPeer(1, 1), null, new byte[] { 1 }));
        Assert.False(second.Emit(129, new NethernetPeer(1, 1), null, new byte[] { 1 }));
        Assert.Equal(1, received);
    }
}
