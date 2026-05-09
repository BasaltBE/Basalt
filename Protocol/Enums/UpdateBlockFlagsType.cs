using System;

namespace Basalt.Protocol.Enums;

[Flags]
public enum UpdateBlockFlagsType : uint
{
    None = 0,
    Neighbors = 1,
    Network = 2,
    NoGraphic = 4,
    Priority = 8
}
