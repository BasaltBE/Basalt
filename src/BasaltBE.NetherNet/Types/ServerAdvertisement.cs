namespace BasaltBE.NetherNet.Types;

public sealed record ServerAdvertisement {
    public required string Name { get; init; }
    public required int Protocol { get; init; }
    public required string Version { get; init; }
    public required string Level { get; init; }
    public int Players { get; init; }
    public int MaxPlayers { get; init; }
    public int GameType { get; init; }
    public bool EditorWorld { get; init; }
    public bool Hardcore { get; init; }
    public bool AcceptsOnlineAuth { get; init; } = true;
    public bool AcceptsSelfSignedAuth { get; init; } = false;
    public string Nonce { get; init; } = "";
    public int ConnectionType { get; init; } = 4;
}

