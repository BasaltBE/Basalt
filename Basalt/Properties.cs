namespace Basalt.Core;

public class Properties {
    [ServerProperties.PropertyOrder(1)]
    [ServerProperties.PropertyCategory("Network")]
    [ServerProperties.PropertyKey("server-port")]
    [ServerProperties.PropertyComment("IPv4 port the server should listen to.")]
    public ushort Port { get; set; } = 19132;

    [ServerProperties.PropertyOrder(2)]
    [ServerProperties.PropertyCategory("Network")]
    [ServerProperties.PropertyKey("server-ipv6-port")]
    [ServerProperties.PropertyComment("IPv6 port the server should listen to.")]
    public ushort IPv6Port { get; set; } = 19133;

    [ServerProperties.PropertyOrder(3)]
    [ServerProperties.PropertyCategory("Network")]
    [ServerProperties.PropertyKey("motd")]
    [ServerProperties.PropertyComment("Message shown in the server list.")]
    public string Motd { get; set; } = "Basalt";

    [ServerProperties.PropertyOrder(4)]
    [ServerProperties.PropertyCategory("Network")]
    [ServerProperties.PropertyKey("raknet-mtu")]
    [ServerProperties.PropertyComment("Maximum transmission unit for RakNet.")]
    public ushort Mtu { get; set; } = 1492;

    [ServerProperties.PropertyOrder(5)]
    [ServerProperties.PropertyCategory("Network")]
    [ServerProperties.PropertyKey("nethernet-enabled")]
    [ServerProperties.PropertyComment("If true, also accept NetherNet WebRTC connections.")]
    public bool NetherNetEnabled { get; set; } = false;

    [ServerProperties.PropertyOrder(6)]
    [ServerProperties.PropertyCategory("Network")]
    [ServerProperties.PropertyKey("online-mode")]
    [ServerProperties.PropertyComment("If true all connected players must be authenticated.")]
    public bool OnlineMode { get; set; } = true;

    [ServerProperties.PropertyOrder(7)]
    [ServerProperties.PropertyCategory("Network")]
    [ServerProperties.PropertyKey("compression-algorithm")]
    [ServerProperties.PropertyComment("Allowed values: zlib, snappy.")]
    public string CompressionMethod { get; set; } = "zlib";

    [ServerProperties.PropertyOrder(8)]
    [ServerProperties.PropertyCategory("Network")]
    [ServerProperties.PropertyKey("compression-threshold")]
    [ServerProperties.PropertyComment("Smallest payload size to compress.")]
    public int CompressionThreshold { get; set; } = 1;

    [ServerProperties.PropertyOrder(9)]
    [ServerProperties.PropertyCategory("RCON")]
    [ServerProperties.PropertyKey("rcon-port")]
    [ServerProperties.PropertyComment("TCP port for the custom RCON endpoint. Set to 0 to disable it.")]
    public ushort RconPort { get; set; } = 25575;

    [ServerProperties.PropertyOrder(10)]
    [ServerProperties.PropertyCategory("RCON")]
    [ServerProperties.PropertyKey("rcon-password")]
    [ServerProperties.PropertyComment("Password for the custom RCON endpoint. Empty disables it.")]
    public string RconPassword { get; set; } = string.Empty;

    [ServerProperties.PropertyOrder(11)]
    [ServerProperties.PropertyCategory("Gameplay")]
    [ServerProperties.PropertyKey("max-players")]
    [ServerProperties.PropertyComment("The maximum number of players that can play on the server.")]
    public int MaxPlayers { get; set; } = 100;

    [ServerProperties.PropertyOrder(12)]
    [ServerProperties.PropertyCategory("Gameplay")]
    [ServerProperties.PropertyKey("achievements-enabled")]
    [ServerProperties.PropertyComment("If true, achievements are earnable but TAB autocomplete is limited.")]
    public bool AchievementsEnabled { get; set; } = false;

    [ServerProperties.PropertyOrder(13)]
    [ServerProperties.PropertyCategory("World")]
    [ServerProperties.PropertyKey("max-view-distance")]
    [ServerProperties.PropertyComment("Maximum chunk view distance players can request.")]
    public int MaxViewDistance { get; set; } = 32;

    [ServerProperties.PropertyOrder(14)]
    [ServerProperties.PropertyCategory("World")]
    [ServerProperties.PropertyKey("simulation-distance")]
    [ServerProperties.PropertyComment("Chunk distance around players where entities are ticked.")]
    public int SimulationDistance { get; set; } = 4;

    [ServerProperties.PropertyOrder(15)]
    [ServerProperties.PropertyCategory("World")]
    [ServerProperties.PropertyKey("chunks-per-tick")]
    [ServerProperties.PropertyComment("Maximum chunks each player can request and receive per tick.")]
    public int ChunksPerTick { get; set; } = 32;

    [ServerProperties.PropertyOrder(16)]
    [ServerProperties.PropertyCategory("World")]
    [ServerProperties.PropertyKey("world-provider")]
    [ServerProperties.PropertyComment("World provider type.")]
    public string WorldProvider { get; set; } = "leveldb";

    [ServerProperties.PropertyOrder(17)]
    [ServerProperties.PropertyCategory("World")]
    [ServerProperties.PropertyKey("world-path")]
    [ServerProperties.PropertyComment("Directory containing worlds.")]
    public string WorldPath { get; set; } = "worlds";

    [ServerProperties.PropertyOrder(18)]
    [ServerProperties.PropertyCategory("World")]
    [ServerProperties.PropertyKey("default-world")]
    [ServerProperties.PropertyComment("Identifier of the default world.")]
    public string DefaultWorldIdentifier { get; set; } = "world";

    [ServerProperties.PropertyOrder(19)]
    [ServerProperties.PropertyCategory("Plugins")]
    [ServerProperties.PropertyKey("plugins-directory")]
    [ServerProperties.PropertyComment("Directory where plugin DLLs are loaded from.")]
    public string PluginsDirectory { get; set; } = "plugins";

    [ServerProperties.PropertyOrder(20)]
    [ServerProperties.PropertyCategory("Plugins")]
    [ServerProperties.PropertyKey("crash-on-plugin-load-failure")]
    [ServerProperties.PropertyComment("If true, stop the server when a plugin fails to load.")]
    public bool CrashOnPluginLoadFailure { get; set; } = false;

    [ServerProperties.PropertyOrder(21)]
    [ServerProperties.PropertyCategory("Performance")]
    [ServerProperties.PropertyKey("worker-threads")]
    [ServerProperties.PropertyComment("Number of worker threads for async task processing.")]
    public int WorkerThreads { get; set; } = 4;

    [ServerProperties.PropertyOrder(22)]
    [ServerProperties.PropertyCategory("Resource Packs")]
    [ServerProperties.PropertyKey("resource-packs-path")]
    [ServerProperties.PropertyComment("Directory containing resource pack folders.")]
    public string ResourcePacksPath { get; set; } = "resource_packs";

    [ServerProperties.PropertyOrder(23)]
    [ServerProperties.PropertyCategory("Resource Packs")]
    [ServerProperties.PropertyKey("force-resource-packs")]
    [ServerProperties.PropertyComment("If true, clients must accept resource packs to join.")]
    public bool ForceResourcePacks { get; set; } = false;

    [ServerProperties.PropertyOrder(24)]
    [ServerProperties.PropertyCategory("Storage")]
    [ServerProperties.PropertyKey("player-data-path")]
    [ServerProperties.PropertyComment("Directory containing server player data.")]
    public string PlayerDataPath { get; set; } = "worlds/players";
}
