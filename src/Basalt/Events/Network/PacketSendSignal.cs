namespace Basalt.Core.Events;

using Basalt.Core.Player;
using Basalt.Core.Network;
using Basalt.BedrockProtocol.Packets;

/// <summary>
/// Emitted before a packet is sent to a client.
/// </summary>
public sealed class PacketSendSignal : ISignal {
    public ServerEvent Event => ServerEvent.PacketSend;
    public NetworkConnection Connection { get; }
    public Player? Player { get; }
    public Packet Packet { get; }
    public bool Cancelled { get; private set; }

    public PacketSendSignal(NetworkConnection connection, Player? player, Packet packet) {
        Connection = connection;
        Player = player;
        Packet = packet;
    }

    public void Cancel() {
        Cancelled = true;
    }
}
