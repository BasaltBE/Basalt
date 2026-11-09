using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Collections.Concurrent;
using System.Threading.Channels;
using BasaltBE.Basalt.Commands;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Network.Events;
using BasaltBE.Basalt.Worlds.Ticking;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Protocol.Packets;
using BasaltBE.NetherNet.Types;
using BasaltBE.NetherNet.Enums;
using BasaltBE.NetherNet;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class ThreadedTickingTests {
    [Fact]
    public async Task RealTransportRoutesEachConnectionToItsWorldWorker() {
        using TcpListener reservation = new(IPAddress.Loopback, 0);
        reservation.Start();
        ushort port = (ushort)((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        using MultiplayerFixture fixture = new(port);
        World other = fixture.Server.CreateWorld("other", world => world.CreateDimension("minecraft:overworld"));
        var dimension = other.GetDimension("minecraft:overworld")!;
        NethernetServer transport = (NethernetServer)typeof(Network.NetworkServer)
            .GetField("nethernet", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(fixture.Server.Network)!;
        Channel<NethernetPeer> connected = Channel.CreateUnbounded<NethernetPeer>();
        transport.Start(message => {
            if (message.Connected)
                connected.Writer.TryWrite(message.Peer);
            fixture.Server.Network.OnMessage(message);
        });
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(20));
        NethernetClientOptions options = new() { SignalingUri = new Uri($"http://127.0.0.1:{port}") };
        await using NethernetClient first = new(options);
        await using NethernetClient second = new(options);
        await first.ConnectAsync(timeout.Token);
        Player one = fixture.CreatePlayer(await connected.Reader.ReadAsync(timeout.Token));
        one.Spawned = false;
        one.Listed = false;
        fixture.Server.Network.AddPlayer(one);
        await second.ConnectAsync(timeout.Token);
        Player two = new(fixture.Server.Network, other, dimension, await connected.Reader.ReadAsync(timeout.Token),
            "Other", "2", Guid.NewGuid(), 8, "test", []);
        fixture.Server.Network.AddPlayer(two);
        using CountdownEvent completed = new(6);
        ConcurrentQueue<(Player Player, int Thread, bool Value)> received = new();
        fixture.Server.Network.Events.Subscribe(new PacketEvent<ClientCacheStatusPacket>(1000), (_, player, packet) => {
            received.Enqueue((player!, Environment.CurrentManagedThreadId, packet.SupportsCache));
            completed.Signal();
        });
        fixture.Server.Started = true;
        fixture.World.Started = true;
        other.Started = true;
        using WorldTickWorker firstWorker = new(fixture.Server, 0, [fixture.World]);
        using WorldTickWorker secondWorker = new(fixture.Server, 1, [other]);
        firstWorker.Start();
        secondWorker.Start();
        for (int index = 0; index < 3; index++) {
            await first.SendAsync(new byte[] { 255, 3, 232, 7, (byte)(index % 2) }, cancellationToken: timeout.Token);
            await second.SendAsync(new byte[] { 255, 3, 232, 7, (byte)(index % 2) }, cancellationToken: timeout.Token);
        }
        Assert.True(completed.Wait(TimeSpan.FromSeconds(10)));
        Assert.All(received, entry => Assert.Equal(entry.Player.World.OwnerThread, entry.Thread));
        Assert.NotEqual(fixture.World.OwnerThread, other.OwnerThread);
        Assert.Equal(new[] { false, true, false }, received.Where(entry => entry.Player == one).Select(entry => entry.Value));
        Assert.Equal(new[] { false, true, false }, received.Where(entry => entry.Player == two).Select(entry => entry.Value));
    }

    [Fact]
    public void InactiveItemKeepsItsRemainingLifetime() {
        using MultiplayerFixture fixture = new();
        fixture.Dimension.GetOrCreateChunk(0, 0);
        Entity source = new();
        ItemEntity item = new(new ItemStack("minecraft:stick"), source);
        fixture.Dimension.AddEntity(item);
        item.SimulationAge = 5998;
        item.SetFlag(ActorFlag.HasGravity, false);
        EntityMovementTrait movement = item.GetTrait<EntityMovementTrait>()!;
        movement.Motion.X = movement.Motion.Y = movement.Motion.Z = 0;
        fixture.Dimension.AddTickingArea("active", [new()]);
        fixture.World.Tick();
        Assert.Equal(5999UL, item.SimulationAge);
        fixture.Dimension.RemoveTickingArea("active");
        for (int tick = 0; tick < 100; tick++)
            fixture.World.Tick();
        Assert.Equal(5999UL, item.SimulationAge);
        Assert.Same(item, fixture.Dimension.GetEntity(item.RuntimeId));
        fixture.Dimension.AddTickingArea("active", [new()]);
        fixture.World.Tick();
        Assert.Null(fixture.Dimension.GetEntity(item.RuntimeId));
    }

    [Fact]
    public void GlobalCommandWaitsWithoutBlockingConcurrentCommandsAndRunsOnce() {
        using MultiplayerFixture fixture = new(ticking: "world");
        ProbeCommand mutation = new("mutation", false, () => fixture.Dimension.GetOrCreateChunk(0, 0));
        ProbeCommand concurrent = new("concurrent", true, () => { });
        fixture.Server.Commands.Register(mutation);
        fixture.Server.Commands.Register(concurrent);
        fixture.Server.Started = true;
        fixture.World.Started = true;
        fixture.World.OwnerThread = -1;
        using ManualResetEventSlim entered = new();
        using ManualResetEventSlim release = new();
        Thread owner = new(() => {
            fixture.Server.Gameplay.EnterReadLock();
            entered.Set();
            release.Wait();
            fixture.Server.Gameplay.ExitReadLock();
        });
        owner.Start();
        Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
        try {
            List<CommandResult> results = [];
            ServerCommandExecutor console = new(fixture.Server);
            Assert.True(fixture.Server.Commands.Enqueue("mutation", console, results.Add));
            Assert.True(fixture.Server.Commands.Enqueue("concurrent", console, results.Add));
            fixture.Server.Commands.Tick();
            Assert.Equal(0, mutation.Calls);
            Assert.Equal(1, concurrent.Calls);
            Assert.Single(results);
            release.Set();
            owner.Join();
            fixture.Server.Commands.Tick();
            fixture.Server.Commands.Tick();
            Assert.Equal(1, mutation.Calls);
            Assert.Equal(2, results.Count);
            Assert.All(results, result => Assert.True(result.Success));
        }
        finally {
            release.Set();
            owner.Join();
        }
    }

    [Fact]
    public void AreasOverlapWithoutDoubleTicksAndInactiveAgePauses() {
        using MultiplayerFixture fixture = new();
        Entity entity = new();
        fixture.Dimension.AddEntity(entity);
        fixture.Dimension.GetOrCreateChunk(0, 0);
        fixture.World.Tick();
        Assert.Equal(0UL, entity.SimulationAge);
        fixture.Dimension.AddTickingArea("one", [new()]);
        fixture.Dimension.AddTickingArea("two", [new()]);
        fixture.World.Tick();
        Assert.Equal(1UL, entity.SimulationAge);
        fixture.Dimension.RemoveTickingArea("one");
        fixture.World.Tick();
        Assert.Equal(2UL, entity.SimulationAge);
        fixture.Dimension.RemoveTickingArea("two");
        for (int index = 0; index < 100; index++)
            fixture.World.Tick();
        Assert.Equal(2UL, entity.SimulationAge);
    }

    [Fact]
    public void CrossingIntoAnotherActiveBucketTicksOnceAndRemovalDoesNotSkipSurvivors() {
        using MultiplayerFixture fixture = new();
        fixture.Dimension.GetOrCreateChunk(0, 0);
        fixture.Dimension.GetOrCreateChunk(1, 0);
        fixture.Dimension.AddTickingArea("both", [new(), new() { X = 1 }]);
        Entity moving = new();
        moving.AddTrait(new CrossChunk(moving));
        fixture.Dimension.AddEntity(moving);
        Entity removed = new();
        removed.AddTrait(new RemoveSelf(removed));
        fixture.Dimension.AddEntity(removed);
        Entity survivor = new();
        fixture.Dimension.AddEntity(survivor);
        fixture.World.Tick();
        Assert.Equal(1UL, moving.SimulationAge);
        Assert.Equal(1UL, survivor.SimulationAge);
        Assert.Null(fixture.Dimension.GetEntity(removed.RuntimeId));
        Assert.Contains(moving, fixture.Dimension.EntityIndex.GetChunkEntities(1, 0).ToArray());
        fixture.World.Tick();
        Assert.Equal(2UL, moving.SimulationAge);
    }

    [Fact]
    public void SlowWorldDoesNotStopAnotherWorkerAndOwnershipIsChecked() {
        using MultiplayerFixture fixture = new();
        World healthy = fixture.Server.CreateWorld("healthy", world => world.CreateDimension("minecraft:overworld"));
        fixture.Dimension.GetOrCreateChunk(0, 0);
        fixture.Dimension.AddTickingArea("slow", [new()]);
        using ManualResetEventSlim blocked = new();
        using ManualResetEventSlim release = new();
        Entity entity = new();
        entity.AddTrait(new BlockingTick(entity, blocked, release));
        fixture.Dimension.AddEntity(entity);
        fixture.World.Started = true;
        healthy.Started = true;
        using WorldTickWorker slow = new(fixture.Server, 0, [fixture.World]);
        using WorldTickWorker fast = new(fixture.Server, 1, [healthy]);
        try {
            slow.Start();
            fast.Start();
            Assert.True(blocked.Wait(TimeSpan.FromSeconds(5)), "Slow worker did not enter its entity tick.");
            Assert.Throws<InvalidOperationException>(() => fixture.Dimension.GetOrCreateChunk(2, 2));
            ulong initial = Volatile.Read(ref healthy.TickValue);
            Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref healthy.TickValue) >= initial + 5,
                TimeSpan.FromSeconds(5)), "Healthy world did not progress while the other worker was blocked.");
            Assert.Equal(1UL, Volatile.Read(ref fixture.World.TickValue));
        } finally {
            release.Set();
            slow.Stop();
            fast.Stop();
            slow.Dispose();
            fast.Dispose();
            fixture.World.Started = false;
            healthy.Started = false;
        }
    }

    [Fact]
    public void IngressOwnsItsBytesAndRunsInOrderOnTheWorldThread() {
        using MultiplayerFixture fixture = new();
        Player player = fixture.CreatePlayer();
        player.Spawned = false;
        fixture.Server.Network.AddPlayer(player);
        List<bool> received = [];
        fixture.Server.Network.Events.Subscribe(new PacketEvent<ClientCacheStatusPacket>(1000),
            (_, _, packet) => received.Add(packet.SupportsCache));
        byte[] bytes = [255, 3, 232, 7, 1];
        Assert.True(fixture.World.Enqueue(new NethernetMessage(player.Peer, MessageReliability.Reliable, bytes)));
        bytes[^1] = 0;
        Assert.True(fixture.World.Enqueue(new NethernetMessage(player.Peer, MessageReliability.Reliable, bytes)));
        for (int tick = 0; tick < 10 && received.Count < 2; tick++)
            fixture.World.Tick();
        Assert.Equal(new[] { true, false }, received);
    }

    private sealed class CrossChunk(Entity entity) : EntityTrait(entity) {
        public override void Tick() {
            Entity.Position.X = 16;
            Entity.OnMove();
        }
    }

    private sealed class RemoveSelf(Entity entity) : EntityTrait(entity) {
        public override void Tick() => Entity.Despawn();
    }

    private sealed class BlockingTick(Entity entity, ManualResetEventSlim entered, ManualResetEventSlim release) : EntityTrait(entity) {
        public override void Tick() {
            entered.Set();
            release.Wait();
        }
    }

    private sealed class ProbeCommand : Command {
        private readonly Action action;
        internal int Calls;

        internal ProbeCommand(string name, bool concurrent, Action action) : base(name, name) {
            Concurrent = concurrent;
            this.action = action;
            AddOverload();
        }

        public override CommandResult Execute(CommandContext context) {
            Calls++;
            action();
            return new(true, "Done.");
        }
    }
}

