namespace Basalt.Protocol.Nbt;

public readonly record struct ReadWriteOptions(bool Name = true, bool Type = true, bool VarInt = false);
