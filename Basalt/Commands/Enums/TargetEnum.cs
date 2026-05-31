namespace Basalt.Server.Commands;

public class TargetEnum : CommandEnum
{
    public string Raw = string.Empty;

    public TargetEnum() : base("target") { }

    public TargetEnum(string raw, Basalt.Server.Entity.Entity[] entities, string[]? offlineUsernames = null) : base("target")
    {
        Raw = raw;
        Entities = entities;
        OfflineUsernames = offlineUsernames ?? [];
    }

    public Basalt.Server.Entity.Entity[] Entities = [];
    public string[] OfflineUsernames = [];
}







