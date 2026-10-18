using System.Net;
using System.Diagnostics;
using BasaltBE.Logging;
using BasaltBE.Basalt.Commands;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.BlockActors;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Events;
using BasaltBE.Basalt.LootTables;
using BasaltBE.Basalt.Network;
using BasaltBE.Basalt.Network.ResourcePacks;
using BasaltBE.Basalt.Permissions;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Basalt.Properties;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Ticking;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.NetherNet;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt;

public sealed class Server : IDisposable
{
    private int stopping;
    private int disposed;
    private int coordinatorThread;
    internal bool Started;
    internal readonly ReaderWriterLockSlim Gameplay = new();
    private WorldTickWorker[] workers = [];
    private ChunkGenerationService? generation;
    public readonly ServerProperties Properties;
    public readonly NetworkServer Network;
    public readonly EventEmitter Events = new();
    public readonly ChunkGeneratorRegistry Generators = new();
    public readonly LootTableManager LootTables = new();
    public readonly BlockActorRegistry BlockActors = new();
    private readonly List<World> worlds = [];
    public readonly IReadOnlyList<World> Worlds;
    public readonly CommandRegistry Commands;
    public readonly PermissionStore Permissions;
    internal ulong TickValue;
    public readonly TickMetrics Ticks = new();
    public bool Stopping => Volatile.Read(ref stopping) != 0;

    public Server(ServerProperties properties, IServerIdentity? identity = null, PermissionStore? permissions = null)
    {
        Worlds = worlds.AsReadOnly();
        Commands = new CommandRegistry(this);
        Permissions = permissions ?? new PermissionStore();
        Properties = properties;
        ArgumentException.ThrowIfNullOrWhiteSpace(Properties.DefaultWorldName);

        Network = new NetworkServer(this, new NethernetServerOptions
        {
            Advertisement = new ServerAdvertisement
            {
                Name = Properties.ServerName,
                Protocol = BedrockProtocol.ProtocolVersion,
                Version = BedrockProtocol.MinecraftVersion,
                Level = Properties.LevelName,
                MaxPlayers = Properties.MaxPlayers
            },
            HttpAddress = new IPEndPoint(IPAddress.Any, Properties.ServerPort),
            DiscoveryAddress = new IPEndPoint(IPAddress.Any, Properties.DiscoveryPort),
            IdentityKeyPath = Properties.IdentityKeyPath
        }, identity, ResourcePackManager.Load(Properties.ResourcePacksPath));
    }

    public World CreateWorld(string identifier, Action<World> configure)
    {
        if (Started)
            throw new InvalidOperationException("Create worlds before starting the server.");
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(configure);
        foreach (World existing in Worlds)
        {
            if (string.Equals(existing.Identifier, identifier, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"World '{identifier}' already exists.");
        }

        World world = new(identifier, Generators, generation ??= new ChunkGenerationService(4, 1024), LootTables);
        world.Server = this;
        worlds.Add(world);
        try
        {
            world.Initialize(configure, Events);
        }
        catch
        {
            worlds.Remove(world);
            world.Dispose();
            throw;
        }
        return world;
    }

    public World GetWorld() => GetWorld(Properties.DefaultWorldName);

    public World GetWorld(string identifier)
    {
        foreach (World world in worlds)
        {
            if (string.Equals(world.Identifier, identifier, StringComparison.OrdinalIgnoreCase))
                return world;
        }

        throw new KeyNotFoundException($"World '{identifier}' was not found.");
    }

    public Player? GetPlayer(string username)
    {
        foreach (World world in worlds)
        {
            Player? player = world.GetPlayer(username);
            if (player is not null)
                return player;
        }

        return null;
    }

    public Player? GetPlayerByXuid(string xuid)
    {
        foreach (World world in worlds)
        {
            Player? player = world.GetPlayerByXuid(xuid);
            if (player is not null)
                return player;
        }

        return null;
    }

    public void Broadcast(DataPacket packet, Player? exclude = null)
    {
        ArgumentNullException.ThrowIfNull(packet);
        foreach (World world in worlds)
        {
            foreach (Dimension dimension in world.Dimensions.Values)
                dimension.Broadcast(packet, exclude);
        }
    }

    public void Start()
    {
        if (Started)
            throw new InvalidOperationException("The server has already started.");
        if (Properties.Ticking is not ("default" or "world"))
            throw new InvalidOperationException("Ticking must be default or world. Dimension mode is not implemented yet.");
        if (Properties.TickingThreads is < 1 or > 64 || Properties.TickDistance is < 0 or > 32)
            throw new InvalidOperationException("Ticking threads must be 1 to 64 and tick distance must be 0 to 32.");
        if (Worlds.Count == 0)
            throw new InvalidOperationException("Create a world before starting the server.");

        _ = GetWorld();
        Commands.Prepare();
        BlockActors.Freeze();
        coordinatorThread = Environment.CurrentManagedThreadId;
        Started = true;
        foreach (World world in worlds)
        {
            world.Started = true;
            world.OwnerThread = Environment.CurrentManagedThreadId;
        }
        if (Properties.Ticking == "world")
        {
            int count = Math.Min(Properties.TickingThreads, Worlds.Count);
            workers = new WorldTickWorker[count];
            for (int index = 0; index < count; index++)
            {
                World[] assigned = worlds.Where((_, worldIndex) => worldIndex % count == index).ToArray();
                workers[index] = new WorldTickWorker(this, index, assigned);
            }
            foreach (WorldTickWorker worker in workers)
                worker.Start();
            Logger.Scoped("Ticking").Info($"World ticking uses {workers.Length} persistent worker(s).");
        }

        Network.Start();
    }

    public void Stop()
    {
        Interlocked.Exchange(ref stopping, 1);
    }

    public void Tick()
    {
        if (Started && Environment.CurrentManagedThreadId != coordinatorThread)
            throw new InvalidOperationException("Server.Tick must run on the server thread.");
        long start = Stopwatch.GetTimestamp();
        using ProfilerZone zone = Profiler.BeginZone("Server.Tick");
        TickValue++;
        if (workers.Length == 0)
        {
            Commands.Tick();
            foreach (World world in worlds)
                world.Tick();
        }
        else
        {
            Poll();
        }
        Profiler.FrameMark();
        Ticks.Record(start);
    }

    public void Poll()
    {
        if (Started && Environment.CurrentManagedThreadId != coordinatorThread)
            throw new InvalidOperationException("Server.Poll must run on the server thread.");
        if (workers.Length == 0)
            return;
        Commands.Tick();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;
        foreach (WorldTickWorker worker in workers)
            worker.Stop();
        foreach (WorldTickWorker worker in workers)
            worker.Dispose();
        Gameplay.EnterWriteLock();
        try
        {
            Network.Dispose();
            foreach (World world in worlds)
                world.Dispose();
            generation?.Dispose();
        }
        finally
        {
            Gameplay.ExitWriteLock();
        }
        Events.Clear();
        Gameplay.Dispose();
    }
}
