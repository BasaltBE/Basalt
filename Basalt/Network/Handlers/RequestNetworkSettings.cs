using Basalt.Core;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.RakNet;
using BinaryReader = Basalt.Binary.BinaryReader;

namespace Basalt.Network.Handlers;

public static class RequestNetworkSettings
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        RequestNetworkSettingsPacket packet = new();
        packet.Deserialize(packetBuffer);
        Console.WriteLine($"RequestNetworkSettings protocol={packet.ProtocolVersion}");

        NetworkSettingsPacket response = new(
            compressionThreshold: server.Options.CompressionThreshold,
            compressionMethod: server.Options.CompressionMethod,
            clientThrottle: false,
            clientThrottleThreshold: 0,
            clientThrottleScalar: 0f
        );

        server.Network.SendPacket(connection, response, CompressionMethod.NotPresent);
    }
}
