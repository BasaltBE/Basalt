using System.IO.Compression;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Channels;
using Basalt.Binary;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Traits;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.NetherNet;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Xunit;

namespace BasaltBE.Basalt.Tests;

[Collection("Block trait registrations")]
public sealed class MultiplayerTransportTests {
    readonly Dictionary<NethernetClient, Queue<(uint Id, byte[] Payload)>> receivedPackets = [];

    [Fact]
    public async Task ScheduledButtonUpdateAndSoundReachItsChunkViewer() {
        using TcpListener reservation = new(IPAddress.Loopback, 0);
        reservation.Start();
        ushort port = (ushort)((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        using MultiplayerFixture fixture = new(port);
        var network = fixture.Server.Network;
        NethernetServer transport = (NethernetServer)typeof(Network.NetworkServer)
            .GetField("nethernet", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(network)!;
        Channel<NethernetPeer> connected = Channel.CreateUnbounded<NethernetPeer>();
        transport.Start(message => {
            if (message.Connected)
                connected.Writer.TryWrite(message.Peer);
        });

        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(45));
        await using NethernetClient client = new(new NethernetClientOptions {
            SignalingUri = new Uri($"http://127.0.0.1:{port}")
        });
        await client.ConnectAsync(timeout.Token);
        Player player = fixture.CreatePlayer(await connected.Reader.ReadAsync(timeout.Token));
        network.AddPlayer(player);
        fixture.Dimension.AddChunkViewer(player, 0, 0);
        fixture.Dimension.Tick();
        Assert.Equal(29u, (await ReceivePacket(client, timeout.Token)).Id);

        Stopwatch chunkWait = Stopwatch.StartNew();
        while (fixture.Dimension.GetChunk(0, 0) is null && chunkWait.Elapsed < TimeSpan.FromSeconds(5)) {
            fixture.Dimension.Tick();
            await Task.Delay(1, timeout.Token);
        }
        Assert.NotNull(fixture.Dimension.GetChunk(0, 0));

        BlockPermutation button = BlockPalette.FindDefaultPermutation("minecraft:stone_button")!;
        BlockPermutation pressed = BlockPalette.Find(button.Identifier, button.State, "button_pressed_bit", true)!;
        fixture.Dimension.SetPermutation(2, 10, 2, pressed);
        fixture.Dimension.AddTickingArea("test", [new ChunkPos { X = 0, Z = 0 }]);
        fixture.Dimension.ScheduleBlockUpdate(2, 10, 2, 2);
        fixture.Dimension.Tick();
        fixture.Dimension.Tick();

        (uint updateId, byte[] updatePayload) = await ReceivePacket(client, timeout.Token);
        while (updateId == 39u)
            (updateId, updatePayload) = await ReceivePacket(client, timeout.Token);
        Assert.Equal(21u, updateId);
        int updateOffset = 0;
        BinaryReader updateReader = new(updatePayload, ref updateOffset);
        UpdateBlockPacket update = new();
        update.Deserialize(ref updateReader);
        Assert.Equal(updatePayload.Length, updateOffset);
        Assert.Equal((2, 10, 2), (update.Position.X, update.Position.Y, update.Position.Z));
        Assert.Equal(unchecked((uint)BlockPalette.Find(button.Identifier, button.State, "button_pressed_bit", false)!.Hash), update.BlockRuntimeId);

        (uint soundId, byte[] soundPayload) = await ReceivePacket(client, timeout.Token);
        Assert.Equal(123u, soundId);
        int soundOffset = 0;
        BinaryReader soundReader = new(soundPayload, ref soundOffset);
        LevelSoundEventPacket sound = new();
        sound.Deserialize(ref soundReader);
        Assert.Equal(soundPayload.Length, soundOffset);
        Assert.Equal("random.click", sound.SoundEvent);
        Assert.Equal(2.5f, sound.Position.X);
        Assert.Equal(10.5f, sound.Position.Y);
        Assert.Equal(2.5f, sound.Position.Z);
    }

    [Fact]
    public async Task ThreePeersReceiveBatchedRostersMovementAndOrderedRemoval() {
        using TcpListener reservation = new(IPAddress.Loopback, 0);
        reservation.Start();
        ushort port = (ushort)((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        using MultiplayerFixture fixture = new(port);
        var network = fixture.Server.Network;
        NethernetServer transport = (NethernetServer)typeof(Network.NetworkServer)
            .GetField("nethernet", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(network)!;
        Channel<NethernetPeer> connected = Channel.CreateUnbounded<NethernetPeer>();
        transport.Start(message => {
            if (message.Connected)
                connected.Writer.TryWrite(message.Peer);
        });
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(45));
        NethernetClientOptions options = new() { SignalingUri = new Uri($"http://127.0.0.1:{port}") };
        await using NethernetClient first = new(options);
        await using NethernetClient second = new(options);
        await using NethernetClient third = new(options);
        await first.ConnectAsync(timeout.Token);
        NethernetPeer firstPeer = await connected.Reader.ReadAsync(timeout.Token);

        using BinaryStream skinStream = BinaryStream.Rent(32 * 1024);
        BinaryWriter skinWriter = skinStream.GetWriter();
        new SerializedSkin {
            Id = "transport-skin",
            ImageData = new SkinImage { Width = 64, Height = 64, ImageBytes = new byte[16384] }
        }.Write(ref skinWriter);
        byte[] skin = skinStream.Buffer.Span[..skinStream.Offset].ToArray();

        Player one = fixture.CreatePlayer(firstPeer, skin);
        one.Listed = false;
        network.AddPlayer(one);
        network.UpdatePlayerList(one);
        foreach (Player recipient in fixture.Dimension.GetPlayers()) network.FlushPackets(recipient);
        Assert.Empty(DecodeList(await ReceivePacket(first, timeout.Token)).Entries);

        await second.ConnectAsync(timeout.Token);
        Player two = fixture.CreatePlayer(await connected.Reader.ReadAsync(timeout.Token), skin);
        two.Listed = false;
        network.AddPlayer(two);
        network.UpdatePlayerList(two);
        foreach (Player recipient in fixture.Dimension.GetPlayers()) network.FlushPackets(recipient);
        Assert.Equal(one.Username, Assert.Single(DecodeList(await ReceivePacket(second, timeout.Token)).Entries).AddEntry!.Name);
        Assert.Equal(two.Username, Assert.Single(DecodeList(await ReceivePacket(first, timeout.Token)).Entries).AddEntry!.Name);

        await third.ConnectAsync(timeout.Token);
        Player three = fixture.CreatePlayer(await connected.Reader.ReadAsync(timeout.Token), skin);
        three.Listed = false;
        network.AddPlayer(three);
        network.UpdatePlayerList(three);
        foreach (Player recipient in fixture.Dimension.GetPlayers()) network.FlushPackets(recipient);
        PlayerListPacket roster = DecodeList(await ReceivePacket(third, timeout.Token));
        Assert.Equal(2, roster.Entries.Length);
        Assert.All(roster.Entries, entry => Assert.Equal("transport-skin", entry.AddEntry!.Skin.Id));
        Assert.Equal(new[] { one.Username, two.Username }.Order(), roster.Entries.Select(entry => entry.AddEntry!.Name).Order());
        Assert.Single(DecodeList(await ReceivePacket(first, timeout.Token)).Entries);
        Assert.Single(DecodeList(await ReceivePacket(second, timeout.Token)).Entries);

        PlayerChunkRenderingTrait rendering = new(one);
        one.AddTrait(rendering);
        MultiplayerFixture.LoadedChunks(rendering).Add(0);
        rendering.UpdateEntities([two], packet => network.SendPacket(one, packet));
        network.FlushPackets(one);
        Assert.Equal(12u, (await ReceivePacket(first, timeout.Token)).Id);
        Assert.Equal(187u, (await ReceivePacket(first, timeout.Token)).Id);
        Assert.Equal(39u, (await ReceivePacket(first, timeout.Token)).Id);

        Entity actor = new(new EntityType { Identifier = "minecraft:pig" });
        rendering.UpdateEntities([two, actor], packet => network.SendPacket(one, packet));
        network.FlushPackets(one);
        Assert.Equal(13u, (await ReceivePacket(first, timeout.Token)).Id);

        actor.Position.X = 1;
        for (int tick = 0; tick < 3; tick++)
            rendering.UpdateEntities([two, actor], packet => network.SendPacket(one, packet));
        network.FlushPackets(one);
        (uint movementId, byte[] movementPayload) = await ReceivePacket(first, timeout.Token);
        Assert.Equal(111u, movementId);
        int movementOffset = 0;
        BinaryReader movementReader = new(movementPayload, ref movementOffset);
        MoveActorDeltaPacket movement = new();
        movement.Deserialize(ref movementReader);
        Assert.Equal(movementPayload.Length, movementOffset);
        Assert.Equal(actor.RuntimeId, movement.MoveData.ActorRuntimeId);
        Assert.Equal(1f, movement.MoveData.PositionX);
        Assert.Null(movement.MoveData.PositionY);
        Assert.Null(movement.MoveData.PositionZ);

        rendering.UpdateEntities([two], packet => network.SendPacket(one, packet));
        network.FlushPackets(one);
        Assert.Equal(14u, (await ReceivePacket(first, timeout.Token)).Id);

        network.RemovePlayer(two.Peer);
        network.Tick();
        network.FlushPackets(one);
        Assert.Equal(14u, (await ReceivePacket(first, timeout.Token)).Id);
        PlayerListPacket removal = DecodeList(await ReceivePacket(first, timeout.Token));
        Assert.Equal(PlayerListAction.Remove, Assert.Single(removal.Entries).Action);
        Assert.Equal(two.ListEntry.AddEntry!.Uuid.MostSignificantBits, removal.Entries[0].RemoveEntry!.Uuid.MostSignificantBits);
        Assert.False(rendering.Viewing(two));
    }

    private static PlayerListPacket DecodeList((uint Id, byte[] Payload) packet) {
        Assert.Equal(63u, packet.Id);
        int offset = 0;
        BinaryReader reader = new(packet.Payload, ref offset);
        PlayerListPacket list = new();
        list.Deserialize(ref reader);
        Assert.Equal(packet.Payload.Length, offset);
        return list;
    }

    private async Task<(uint Id, byte[] Payload)> ReceivePacket(NethernetClient client, CancellationToken token) {
        if (!receivedPackets.TryGetValue(client, out var packets)) {
            packets = new();
            receivedPackets.Add(client, packets);
        }
        if (packets.TryDequeue(out var queued)) return queued;
        NethernetMessage message = await client.ReceiveAsync(token);
        Assert.False(message.Disconnected);
        ReadOnlyMemory<byte> payload = message.Payload;
        byte[] batch;
        if (payload.Span[0] == 0) {
            using MemoryStream input = new(payload[1..].ToArray());
            using DeflateStream deflate = new(input, CompressionMode.Decompress);
            using MemoryStream output = new();
            deflate.CopyTo(output);
            batch = output.ToArray();
        }
        else {
            Assert.Equal(0xFF, payload.Span[0]);
            batch = payload[1..].ToArray();
        }
        int offset = 0;
        while (offset < batch.Length) {
            BinaryReader reader = new(batch, ref offset);
            int length = checked((int)reader.ReadVarUInt());
            Assert.InRange(length, 1, reader.Remaining);
            int end = offset + length;
            uint id = reader.ReadVarUInt();
            packets.Enqueue((id, reader.ReadBytes(end - offset).ToArray()));
        }

        return packets.Dequeue();
    }
}
