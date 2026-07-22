namespace Basalt.Core.Events;

using Basalt.Core.Player;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

/// <summary>
/// Emitted before a packet is sent to a client.
/// </summary>
public sealed class PacketSendSignal : ISignal {
    public ServerEvent Event => ServerEvent.PacketSend;
    public NetworkConnection Connection { get; }
    public Player? Player { get; }
    public DataPacket Packet { get; }
    public bool Cancelled { get; private set; }

    public PacketSendSignal(NetworkConnection connection, Player? player, DataPacket packet) {
        Connection = connection;
        Player = player;
        Packet = packet;
    }

    public void Cancel() {
        Cancelled = true;
    }
}
