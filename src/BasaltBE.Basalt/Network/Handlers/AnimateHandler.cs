using BasaltBE.Basalt.Entities;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class AnimateHandler {
    public void Handle(NethernetPeer peer, Player? player, AnimatePacket packet) {
        if (player is null) throw new InvalidDataException("Animate packet arrived before player login.");
        using Lock.Scope tickSync = player.TickSync.EnterScope();
        if (player.Disposed || !player.Spawned || !Enum.IsDefined(packet.Action)) return;

        packet.TargetActorRuntimeId = player.RuntimeId;
        player.Dimension.Broadcast(packet, player.Position, player);
    }
}
