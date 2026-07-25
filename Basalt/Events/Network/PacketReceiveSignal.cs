namespace Basalt.Core.Events;

using Basalt.Core.Player;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

/// <summary>
/// Emitted when a packet is received from a client, before it is handled.
/// </summary>
public sealed class PacketReceiveSignal : ISignal {
    public ServerEvent Event => ServerEvent.PacketReceive;
    public NetworkConnection Connection { get; }
    public Player? Player { get; }
    public PacketId PacketId { get; }
    public ReadOnlyMemory<byte> PacketBuffer { get; }
    public DataPacket Packet { get; }
    public bool Cancelled { get; private set; }

    public PacketReceiveSignal(
        NetworkConnection connection,
        Player? player,
        PacketId packetId,
        ReadOnlyMemory<byte> packetBuffer,
        DataPacket packet) {
        Connection = connection;
        Player = player;
        PacketId = packetId;
        PacketBuffer = packetBuffer;
        Packet = packet;
    }

    public void Cancel() {
        Cancelled = true;
    }
}
