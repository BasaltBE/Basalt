using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
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
    public void PlayStatusPacketRoundTripsLoginSuccess() {
        PlayStatusPacket packet = new() { Status = PlayStatus.LoginSuccess };
        using BinaryStream stream = BinaryStream.Rent(8);
        BinaryWriter writer = stream.GetWriter();
        packet.Serialize(ref writer);
        stream.Offset = 0;
        BinaryReader reader = stream.GetReader();
        PlayStatusPacket decoded = new();

        decoded.Deserialize(ref reader);

        Assert.Equal(PlayStatus.LoginSuccess, decoded.Status);
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

    [Fact]
    public void ResourcePackChunkDataPacketRoundTripsMemoryPayload() {
        ResourcePackChunkDataPacket packet = new() {
            ResourceName = "12345678-1234-1234-1234-123456789012",
            ChunkId = 3,
            ByteOffset = 393_216,
            ChunkData = new byte[] { 1, 2, 3, 4 }
        };
        using BinaryStream stream = BinaryStream.Rent(128);
        BinaryWriter writer = stream.GetWriter();
        packet.Serialize(ref writer);
        stream.Offset = 0;
        BinaryReader reader = stream.GetReader();
        ResourcePackChunkDataPacket decoded = new();

        decoded.Deserialize(ref reader);

        Assert.Equal(packet.ResourceName, decoded.ResourceName);
        Assert.Equal(packet.ChunkId, decoded.ChunkId);
        Assert.Equal(packet.ByteOffset, decoded.ByteOffset);
        Assert.Equal(packet.ChunkData.ToArray(), decoded.ChunkData.ToArray());
    }

    [Fact]
    public void ResourcePackDataInfoPacketRoundTripsTransferMetadata() {
        ResourcePackDataInfoPacket packet = new() {
            ResourceName = "pack-id",
            ChunkSize = 131_072,
            NumberOfChunks = 9,
            FileSize = 1_048_577,
            FileHash = [1, 2, 3, 4],
            IsPremiumPack = false,
            PackType = 6
        };
        using BinaryStream stream = BinaryStream.Rent(128);
        BinaryWriter writer = stream.GetWriter();
        packet.Serialize(ref writer);
        stream.Offset = 0;
        BinaryReader reader = stream.GetReader();
        ResourcePackDataInfoPacket decoded = new();

        decoded.Deserialize(ref reader);

        Assert.Equal(packet.ResourceName, decoded.ResourceName);
        Assert.Equal(packet.ChunkSize, decoded.ChunkSize);
        Assert.Equal(packet.NumberOfChunks, decoded.NumberOfChunks);
        Assert.Equal(packet.FileSize, decoded.FileSize);
        Assert.Equal(packet.FileHash, decoded.FileHash);
        Assert.Equal(packet.PackType, decoded.PackType);
    }

    [Fact]
    public void ResourcePackClientResponsePacketRoundTripsDownloadingPackIds() {
        ResourcePackClientResponsePacket packet = new() {
            Response = new ResourcePackClientResponseDownloading {
                PackIds = ["first-pack", "second-pack"]
            }
        };
        using BinaryStream stream = BinaryStream.Rent(128);
        BinaryWriter writer = stream.GetWriter();
        packet.Serialize(ref writer);
        stream.Offset = 0;
        BinaryReader reader = stream.GetReader();
        ResourcePackClientResponsePacket decoded = new();

        decoded.Deserialize(ref reader);

        ResourcePackClientResponseDownloading downloading = Assert.IsType<ResourcePackClientResponseDownloading>(decoded.Response);
        Assert.Equal(((ResourcePackClientResponseDownloading)packet.Response).PackIds, downloading.PackIds);
    }

    [Fact]
    public void ResourcePackDownloadingResponseRejectsImpossiblePackCount() {
        using BinaryStream stream = BinaryStream.Rent(8);
        BinaryWriter writer = stream.GetWriter();
        writer.WriteVarUInt(uint.MaxValue);
        stream.Offset = 0;
        ResourcePackClientResponseDownloading response = new();

        Assert.Throws<InvalidDataException>(() => {
            BinaryReader reader = stream.GetReader();
            response.Read(ref reader);
        });
    }
}
