using Basalt.Core;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class ItemStackRequest
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        ItemStackRequestPacket packet = new();
        packet.Deserialize(packetBuffer);

        if (!server.Players.ContainsKey(connection))
        {
            return;
        }

        if (packet.Requests.Count == 0)
        {
            return;
        }

        List<ItemStackResponse> responses = new(packet.Requests.Count);
        for (int i = 0; i < packet.Requests.Count; i++)
        {
            responses.Add(new ItemStackResponse
            {
                Status = ItemStackResponseStatus.Error,
                RequestId = packet.Requests[i].RequestId,
                ContainerInfo = []
            });
        }

        ItemStackResponsePacket responsePacket = new()
        {
            Responses = responses
        };

        server.Network.SendPacket(connection, responsePacket);
    }
}
