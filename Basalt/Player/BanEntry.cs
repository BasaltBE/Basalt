namespace Basalt.Core.Player;

public sealed class BanEntry {
    public string Identifier { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Xuid { get; set; } = string.Empty;
    public long Until { get; set; }
    public string Reason { get; set; } = string.Empty;
}
