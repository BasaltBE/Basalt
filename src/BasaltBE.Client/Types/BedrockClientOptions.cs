namespace BasaltBE.Client;

public sealed record BedrockClientOptions {
    public required Uri ServerUri { get; init; }
    public string AuthenticationCachePath { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BasaltBE",
        "auth.json"
    );
    public int ProtocolVersion { get; init; } = Protocol.BedrockProtocol.ProtocolVersion;
}
