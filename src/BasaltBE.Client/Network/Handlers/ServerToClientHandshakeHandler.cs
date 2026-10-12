using BasaltBE.Protocol.Packets;

namespace BasaltBE.Client.Network.Handlers;

internal sealed class ServerToClientHandshakeHandler(BedrockClient client) {
    internal void Handle(ServerToClientHandshakePacket packet) {
        if (string.IsNullOrWhiteSpace(packet.HandshakeWebToken)) {
            throw new InvalidDataException("Bedrock server sent an invalid encryption handshake.");
        }

        client.QueuePacket(new ClientToServerHandshakePacket());
    }
}
