namespace Basalt.Core.Network.Handlers;

using Basalt.BedrockProtocol.Packets;

public static class PacketViolationWarning {
    public static void Handle(Server server, NetworkConnection connection, PacketViolationWarningPacket packet) {
        Logger.Warn(
            $"Packet violation warning: type={packet.ViolationType}, " +
            $"severity={packet.ViolationSeverity}, " +
            $"packetId={packet.ViolationPacketId}, " +
            $"context={packet.ViolationContext}"
        );
    }
}
