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

    [ServerProperty("Resource Packs", 8, "resource-packs-path", "Directory containing resource packs.")]
    public string ResourcePacksPath;

    public ServerProperties() {
        ServerName = "BasaltBE";
        LevelName = "Bedrock level";
        ServerPort = 19132;
        DiscoveryPort = 7551;
        IdentityKeyPath = "nethernet-identity.pem";
        MaxPlayers = 100;
        MaxViewDistance = 32;
        ResourcePacksPath = "resource_packs";
    }
}
