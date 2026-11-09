using BasaltBE.Protocol.Packets;

namespace BasaltBE.Client.Network.Handlers;

internal sealed class DisconnectHandler {
    internal void Handle(DisconnectPacket packet) {
        string reason = packet.HideDisconnectReason || string.IsNullOrWhiteSpace(packet.Message)
            ? packet.Reason.ToString()
            : $"{packet.Reason}: {packet.Message}";
        throw new IOException($"Client disconnected: {reason}.");
    }
}
