namespace Basalt.Protocol.Enums;

public enum CommandParameterType : uint
{
    Int = 1,
    Float = 3,
    Value = 4,
    WildcardInt = 5,
    Operator = 6,
    CompareOperator = 7,
    Target = 8,
    WildcardTarget = 10,
    Filepath = 17,
    FullIntegerRange = 23,
    EquipmentSlot = 47,
    String = 56,
    IntPosition = 64,
    Position = 65,
    Message = 67,
    MessageRoot = 68,
    RawText = 70,
    Json = 74,
    BlockStates = 83,
    BlockStateArray = 84,
    Command = 87
}
