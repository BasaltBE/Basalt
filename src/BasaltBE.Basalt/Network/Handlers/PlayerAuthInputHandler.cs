using BasaltBE.Basalt.Entities;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class PlayerAuthInputHandler {
    public void Handle(NethernetPeer peer, Player? player, PlayerAuthInputPacket packet) {
        if (player is null)
            throw new InvalidDataException("Player auth input arrived before player login.");
        if (player.Disposed)
            return;

        if (
            packet.InputData.Contains(PlayerAuthInputData.PerformItemStackRequest) &&
            packet.ItemStackRequest is { } request
        ){
            ItemStackResponseInfo response = ItemStackRequestHandler.ProcessRequest(player, request);
            player.Network.SendPacket(player, new ItemStackResponsePacket
            {
                Responses = [response]
            });
        }
    }
}
