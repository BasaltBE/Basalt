using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Xunit;

namespace BasaltBE.Protocol.Tests;

public sealed class PacketTests {
    [Fact]
    public void LoginPacketRoundTripsItsFields() {
        LoginPacket packet = new() {
            ClientNetworkVersion = 2225,
            ConnectionRequest = new byte[] { 1, 2, 3, 4 }
        };
        using BinaryStream stream = BinaryStream.Rent(32);
        BinaryWriter writer = stream.GetWriter();
        packet.Serialize(ref writer);
        stream.Offset = 0;
        BinaryReader reader = stream.GetReader();
        LoginPacket decoded = new();

        decoded.Deserialize(ref reader);

        Assert.Equal(packet.ClientNetworkVersion, decoded.ClientNetworkVersion);
        Assert.Equal(packet.ConnectionRequest.ToArray(), decoded.ConnectionRequest.ToArray());
    }

    [Fact]
    public void RequestNetworkSettingsPacketRoundTripsProtocolVersion() {
        RequestNetworkSettingsPacket packet = new() { ClientNetworkVersion = 2225 };
        using BinaryStream stream = BinaryStream.Rent(8);
        BinaryWriter writer = stream.GetWriter();
        packet.Serialize(ref writer);
        stream.Offset = 0;
        BinaryReader reader = stream.GetReader();
        RequestNetworkSettingsPacket decoded = new();

        decoded.Deserialize(ref reader);

        Assert.Equal(packet.ClientNetworkVersion, decoded.ClientNetworkVersion);
    }

    [Fact]
    public void DisconnectPacketRoundTripsReasonAndMessages() {
        DisconnectPacket packet = new() {
            Reason = DisconnectFailReason.OutdatedClient,
            Message = "Unsupported Bedrock version.",
            FilteredMessage = "Unsupported Bedrock version."
        };
        using BinaryStream stream = BinaryStream.Rent(128);
        BinaryWriter writer = stream.GetWriter();
        packet.Serialize(ref writer);
        stream.Offset = 0;
        BinaryReader reader = stream.GetReader();
        DisconnectPacket decoded = new();

        decoded.Deserialize(ref reader);

        Assert.Equal(packet.Reason, decoded.Reason);
        Assert.Equal(packet.Message, decoded.Message);
        Assert.Equal(packet.FilteredMessage, decoded.FilteredMessage);
        Assert.False(decoded.HideDisconnectReason);
    }
}
