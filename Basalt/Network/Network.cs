using Basalt.Core;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

namespace Basalt.Network;

public sealed class NetworkHandler
{
    private readonly Server _server;

    public NetworkHandler(Server server)
    {
        _server = server;

    }

    public void HandlePacket(NetworkConnection _, ReadOnlyMemory<byte> payload)
    {
        if (payload.Length <= 2 || payload.Span[0] != 254)
        {
            return; // Not a game packet or not enough data
        }

        // Skip the Game Packet ID
        int offset = 1;
        var compressionHeader = (CompressionMethod)payload.Span[offset++];
        // Anything above .NotPresent is .None
        if((int)compressionHeader > (int)CompressionMethod.NotPresent) 
            compressionHeader = CompressionMethod.None;

        Console.WriteLine(compressionHeader);




    }
}
