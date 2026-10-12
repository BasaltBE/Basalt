using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Client.Network.Handlers;

internal sealed class NetworkSettingsHandler(BedrockClient client, int protocolVersion) {
    private string? identityToken;
    private string? clientToken;

    internal void SetAuthentication(string identityToken, string clientToken) {
        this.identityToken = identityToken;
        this.clientToken = clientToken;
    }

    internal void Handle(NetworkSettingsPacket packet) {
        client.NetworkSettings = new(packet.CompressionThreshold, packet.CompressionAlgorithm);
        client.Compression = client.NetworkSettings;
        client.QueuePacket(new LoginPacket {
            ClientNetworkVersion = protocolVersion,
            ConnectionRequest = CreateConnectionRequest(
                identityToken ?? throw new InvalidOperationException("Login authentication is not ready."),
                clientToken ?? throw new InvalidOperationException("Login authentication is not ready.")
            )
        });
    }

    private static byte[] CreateConnectionRequest(string identityToken, string clientToken) {
        byte[] identity = JsonSerializer.SerializeToUtf8Bytes(new {
            AuthenticationType = 0,
            Token = identityToken
        });
        byte[] client = Encoding.UTF8.GetBytes(clientToken);
        byte[] request = new byte[sizeof(int) + identity.Length + sizeof(int) + client.Length];
        BinaryPrimitives.WriteInt32LittleEndian(request, identity.Length);
        identity.CopyTo(request.AsSpan(sizeof(int)));

        int clientLengthOffset = sizeof(int) + identity.Length;
        BinaryPrimitives.WriteInt32LittleEndian(request.AsSpan(clientLengthOffset), client.Length);
        client.CopyTo(request.AsSpan(clientLengthOffset + sizeof(int)));
        return request;
    }
}
