namespace BasaltBE.Basalt.Properties;

public struct ServerProperties {
    [ServerProperty("Network", 1, "server-name", "Name shown in the server list.")]
    public string ServerName;

    [ServerProperty("Network", 2, "level-name", "Level name shown to connected players.")]
    public string LevelName;

    [ServerProperty("Network", 3, "server-port", "TCP port used for NetherNet signaling.")]
    public ushort ServerPort;

    [ServerProperty("Network", 4, "nethernet-discovery-port", "UDP port used for NetherNet discovery.")]
    public ushort DiscoveryPort;

    [ServerProperty("Network", 5, "nethernet-identity-path", "Path to the persistent NetherNet identity key.")]
    public string IdentityKeyPath;

    [ServerProperty("Gameplay", 6, "max-players", "Maximum number of players allowed on the server.")]
    public int MaxPlayers;

    [ServerProperty("World", 7, "max-view-distance", "Maximum chunk radius players can request.")]
    public int MaxViewDistance;

    [ServerProperty("World", 8, "default-world", "Name of the world created when the server starts.")]
    public string DefaultWorldName;

    [ServerProperty("World", 9, "world-provider", "Registered chunk storage provider used by the default world.")]
    public string WorldProvider;

    [ServerProperty("Resource Packs", 10, "resource-packs-path", "Directory containing resource packs.")]
    public string ResourcePacksPath;

    [ServerProperty("Ticking", 11, "ticking", "Gameplay scheduling: default or world. Restart required.")]
    public string Ticking;

    [ServerProperty("Ticking", 12, "ticking-threads", "Maximum persistent gameplay workers in world mode. Excludes network and generation threads.")]
    public int TickingThreads;

    [ServerProperty("Ticking", 13, "tick-distance", "Square simulation radius in chunks, separate from render distance.")]
    public int TickDistance;

    [ServerProperty("Storage", 14, "autosave-interval", "Seconds between periodic player saves and world provider flushes. Set to 0 to disable them; dirty chunk saves continue.")]
    public int AutosaveIntervalSeconds;

    public ServerProperties() {
        Ticking = "default";
        TickingThreads = 4;
        TickDistance = 4;
        AutosaveIntervalSeconds = 300;
        ServerName = "BasaltBE";
        LevelName = "Bedrock level";
        ServerPort = 19132;
        DiscoveryPort = 7551;
        IdentityKeyPath = "nethernet-identity.pem";
        MaxPlayers = 100;
        MaxViewDistance = 32;
        DefaultWorldName = "world";
        WorldProvider = "leveldb";
        ResourcePacksPath = "resource_packs";
    }
}
