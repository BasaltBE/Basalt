namespace Basalt.Commands;

public class TargetEnum : CommandEnum
{
    public string Raw = string.Empty;

    public TargetEnum() : base("target") { }

    public TargetEnum(string raw, Basalt.Entity.Entity[] entities, string[]? offlineUsernames = null) : base("target")
    {
        Raw = raw;
        Entities = entities;
        OfflineUsernames = offlineUsernames ?? [];
    }

    public Basalt.Entity.Entity[] Entities = [];
    public string[] OfflineUsernames = [];
}
