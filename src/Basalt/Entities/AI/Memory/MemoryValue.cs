namespace Basalt.Core.Entities.AI.Memory;

public readonly record struct MemoryValue(object Value, ulong? ExpiresAt = null);
