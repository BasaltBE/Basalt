namespace Basalt.Commands;

public class IntEnum : CommandEnum
{
    public int? Value;

    public IntEnum() : base("int") { }

    public IntEnum(int? value) : base("int")
    {
        Value = value;
    }
}
