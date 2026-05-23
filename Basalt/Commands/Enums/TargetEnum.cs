namespace Basalt.Commands;

public class TargetEnum : CommandEnum
{
    public string Raw = string.Empty;

    public TargetEnum() : base("target") { }

    public TargetEnum(string raw, Basalt.Entity.Entity[] entities) : base("target")
    {
        Raw = raw;
        Entities = entities;
    }

    public Basalt.Entity.Entity[] Entities = [];
}
