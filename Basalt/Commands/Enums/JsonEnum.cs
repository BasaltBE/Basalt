namespace Basalt.Commands;

public class JsonEnum : CommandEnum
{
    public string? Value;

    public JsonEnum() : base("json") { }

    public JsonEnum(string? value) : base("json")
    {
        Value = value;
    }
}
