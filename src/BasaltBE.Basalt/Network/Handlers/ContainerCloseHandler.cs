using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class ContainerCloseHandler {
    public void Handle(NethernetPeer peer, Player? player, ContainerClosePacket packet) {
        if (player is null)
            throw new InvalidDataException("Container close arrived before player login.");
        if (player.Disposed)
            return;

        Container? opened;
        lock (player.Sync)
            opened = player.OpenedContainer;

        if (opened is not null && packet.ContainerId == unchecked((byte)(sbyte)opened.ContainerId)) {
            opened.Close(player, serverInitiated: false);
        }
    }
}
