namespace Basalt.Protocol.Enums;

[Flags]
public enum CommandParameterOption : byte {
    None = 0,
    ForceCollapseEnum = 0x1,
    HasEnumConstraint = 0x2,
    AsChainedCommand = 0x4
}
