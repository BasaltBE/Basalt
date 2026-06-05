namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Entity.Traits;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

public static class MobEquipment
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        MobEquipmentPacket packet = (MobEquipmentPacket)Protocol.Io.Packet.Deserialize(reader);

        if (!server.Players.TryGetValue(connection, out global::Basalt.Core.Player.Player? player))
        {
            return;
        }

        if (packet.EntityRuntimeId != 0 && packet.EntityRuntimeId != player.RuntimeId)
        {
            return;
        }

        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (inventory is null)
        {
            return;
        }

        if (packet.HotBarSlot < 9)
        {
            inventory.SetHeldItem(packet.HotBarSlot);
        }
    }
}
