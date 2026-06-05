namespace Basalt.Protocol.Enums;

[Flags]
public enum CommandParameterTypeFlag : uint
{
    None = 0,
    Valid = 0x100000,
    Enum = 0x200000,
    Postfix = 0x1000000,
    SoftEnum = 0x4000000
}
