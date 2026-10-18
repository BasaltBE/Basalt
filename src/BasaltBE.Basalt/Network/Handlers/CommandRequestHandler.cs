using BasaltBE.Basalt.Entities;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class CommandRequestHandler {
    public void Handle(NethernetPeer peer, Player? player, CommandRequestPacket packet) {
        if (player is null || player.Disposed || !player.Spawned)
            return;
        player.Network.Server.Commands.Enqueue(player, packet.Command, packet.Origin);
    }
}
