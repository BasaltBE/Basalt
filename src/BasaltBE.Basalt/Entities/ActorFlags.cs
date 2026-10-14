using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities;

public sealed class ActorFlags {
    private UInt128 value;
    public bool Dirty;

    public bool Get(ActorFlag flag) {
        int index = (int)flag;
        if ((uint)index >= 127)
            throw new ArgumentOutOfRangeException(nameof(flag));
        return (value & ((UInt128)1 << index)) != 0;
    }

    internal bool Set(ActorFlag flag, bool enabled) {
        int index = (int)flag;
        if ((uint)index >= 127)
            throw new ArgumentOutOfRangeException(nameof(flag));

        UInt128 mask = (UInt128)1 << index;
        bool changed = ((value & mask) != 0) != enabled;
        if (enabled)
            value |= mask;
        else
            value &= ~mask;
        Dirty |= changed;
        return changed;
    }

    internal SetActorDataPacket CreateUpdate(ulong runtimeId, ulong tick = 0) {
        Dirty = false;
        return new SetActorDataPacket {
            ActorRuntimeId = runtimeId,
            Tick = tick,
            ActorData = [
                new ActorDataItem { Id = (uint)ActorDataId.Reserved0, Type = DataItemType.Int64, Value = unchecked((long)(ulong)value) },
                new ActorDataItem { Id = (uint)ActorDataId.Reserved092, Type = DataItemType.Int64, Value = unchecked((long)(ulong)(value >> 64)) }
            ]
        };
    }
}
