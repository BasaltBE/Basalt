using System.IO.Compression;
using Basalt.Binary;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Network;
using BasaltBE.Basalt.Network.Events;
using BasaltBE.Basalt.Properties;
using BasaltBE.NetherNet.Enums;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class NetworkServerTests {
    [Fact]
    public void OnMessageDoesNotEmitPlayerPacketsBeforeLogin() {
        using BasaltBE.Basalt.Server server = new(new ServerProperties(), new ServerIdentityStub());
        NetworkServer network = server.Network;
        NethernetPeer peer = new(1, 2);
        network.OnMessage(new NethernetMessage(peer, MessageReliability.Reliable, ReadOnlyMemory<byte>.Empty, Connected: true));
        Player? receivedPlayer = null;
        ClientCacheStatusPacket? receivedPacket = null;
        network.Events.Subscribe(PacketEvents.ClientCacheStatus, (peer, player, packet) => {
            receivedPlayer = player;
            receivedPacket = packet;
        });

        network.OnMessage(new NethernetMessage(peer, MessageReliability.Reliable, new byte[] { 3, 0x81, 1, 1 }));

        Assert.Null(receivedPlayer);
        Assert.Null(receivedPacket);
    }

    [Fact]
    public void OnMessagePreservesDecompressedBatchWhenOutputGrows() {
        using BasaltBE.Basalt.Server server = new(new ServerProperties(), new ServerIdentityStub());
        NetworkServer network = server.Network;
        NethernetPeer peer = new(1, 2);
        byte[] expected = new byte[8192];
        Array.Fill(expected, (byte)'A');
        Player player = new(peer, "test", string.Empty, Guid.Empty, 0, string.Empty, []);
        network.AddPlayer(player);
        byte[]? received = null;
        network.Events.Subscribe(new PacketEvent<LargePacket>(), (_, _, packet) => received = packet.Data);

        using BinaryStream packetStream = BinaryStream.Rent(expected.Length + 8);
        BinaryWriter packetWriter = packetStream.GetWriter();
        packetWriter.WriteVarUInt(201);
        packetWriter.WriteBytes(expected);
        int packetLength = packetStream.Offset;

        using BinaryStream batchStream = BinaryStream.Rent(packetLength + 5);
        BinaryWriter batchWriter = batchStream.GetWriter();
        batchWriter.WriteVarUInt((uint)packetLength);
        batchWriter.WriteBytes(packetStream.Buffer.Span[..packetLength]);

        using MemoryStream compressedStream = new();
        using (DeflateStream deflater = new(compressedStream, CompressionLevel.Fastest, true))
            deflater.Write(batchStream.Buffer.Span[..batchStream.Offset]);
        byte[] compressed = compressedStream.ToArray();
        byte[] payload = new byte[compressed.Length + 1];
        payload[0] = (byte)PacketCompressionAlgorithm.ZLib;
        compressed.CopyTo(payload, 1);

        network.OnMessage(new NethernetMessage(peer, MessageReliability.Reliable, payload));

        Assert.Equal(expected, received);
    }

    private sealed class ServerIdentityStub : IServerIdentity {
        public string AddAssertion(string answerSdp) => answerSdp;
        public void Dispose() { }
    }

    [PacketId(201)]
    private sealed class LargePacket : DataPacket {
        public byte[] Data = [];

        public override void Serialize(ref BinaryWriter writer) => writer.WriteBytes(Data);

        public override void Deserialize(ref BinaryReader reader) => Data = reader.ReadBytes(reader.Remaining).ToArray();
    }
}
