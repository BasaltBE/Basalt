namespace Basalt.Tests;

using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Packets;

public sealed class BlockActorDataPacketTests {
    [Fact]
    public void ActorDataIsDeepCopiedWhenPacketIsCreated() {
        CompoundTag source = new();
        ListTag items = new() { Name = "Items" };
        CompoundTag item = new();
        item.Set("Slot", new ByteTag { Value = 0 });
        items.Values.Add(item);
        source.Set("Items", items);

        BlockActorDataPacket packet = new() { ActorData = source };
        source.Set("Changed", new ByteTag { Value = 1 });
        item.Set("Count", new ByteTag { Value = 64 });

        Assert.Null(packet.ActorData.Get<ByteTag>("Changed"));
        CompoundTag copiedItem = Assert.IsType<CompoundTag>(packet.ActorData.Get<ListTag>("Items")!.Values[0]);
        Assert.Null(copiedItem.Get<ByteTag>("Count"));
    }
}
