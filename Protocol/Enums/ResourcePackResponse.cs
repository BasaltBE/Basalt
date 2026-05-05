namespace Basalt.Protocol.Enums;

public enum ResourcePackResponse : byte
{
    Refused = 1,
    SendPacks = 2,
    HaveAllPacks = 3,
    AllPacksDownloaded = 3,
    Completed = 4
}
