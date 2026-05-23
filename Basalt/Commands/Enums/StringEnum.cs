namespace Basalt.Commands;

public class StringEnum : CommandEnum
{
    public string? Value;

    public StringEnum() : base("string") { }

    public StringEnum(string? value) : base("string")
    {
        Value = value;
    }
}
