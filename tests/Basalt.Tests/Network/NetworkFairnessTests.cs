namespace Basalt.Tests;

using Basalt.Core.Network;
using Basalt.BedrockProtocol.Packets;

public sealed class NetworkFairnessTests {
    [Fact]
    public void InputPacketsUseThePriorityQueueAndLoopsHaveFiniteBudgets() {
        Assert.True(NetworkHandler.IsPriorityPacket(new PlayerAuthInputPacket()));
        Assert.False(NetworkHandler.IsPriorityPacket(null));
        Assert.True(NetworkHandler.IsLowPriorityPacket(new LevelChunkPacket()));
        Assert.False(NetworkHandler.IsLowPriorityPacket(new PlayerAuthInputPacket()));
        Assert.Equal(256, NetworkHandler.MaxIncomingFramesPerTick);
        Assert.Equal(2048, NetworkHandler.MaxIncomingPacketsPerTick);
        Assert.Equal(4096, NetworkHandler.MaxOutgoingPacketsPerTick);
    }
}
