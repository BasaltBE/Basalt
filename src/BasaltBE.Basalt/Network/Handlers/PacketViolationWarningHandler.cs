using BasaltBE.Basalt.Entities;
using BasaltBE.Logging;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class PacketViolationWarningHandler {
    public void Handle(NethernetPeer peer, Player? player, PacketViolationWarningPacket packet) {
        string name = player?.Username ?? peer.ConnectionId.ToString();
        Logger.Scoped("Network").Warn(
            $"Packet violation from {name}: type: {packet.Type}, severity: {packet.Severity}, packet: 0x{packet.PacketId:X}, context: {packet.Context}."
        );
    }
}
