namespace BasaltBE.Protocol.Enums;

[Flags]
public enum UpdateBlockFlags : uint {
    None = 0,
    Neighbors = 1,
    Network = 2,
    NoGraphics = 4,
    Priority = 8
}
