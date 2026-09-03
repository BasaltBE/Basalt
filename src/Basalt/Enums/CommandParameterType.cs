namespace Basalt.Core.Enums;
public enum CommandParameterType : uint {
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
    EquipmentSlot = 43,
    String = 44,
    IntPosition = 52,
    Position = 53,
    Message = 55,
    MessageRoot = 56,
    RawText = 58,
    Json = 62,
    BlockStates = 71,
    BlockStateArray = 72,
    Command = 75
}
