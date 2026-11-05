using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities;

public sealed class ActorFlags {
    private UInt128 value;
    short? airSupply;
    short maximumAirSupply;
    public bool Dirty;

    internal void SetAirSupply(short supply, short maximum) {
        Dirty |= airSupply != supply || maximumAirSupply != maximum;
        airSupply = supply;
        maximumAirSupply = maximum;
    }

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
            ActorData = CreateSnapshot()
        };
    }

    internal ActorDataItem[] CreateSnapshot() {
        ActorDataItem[] data = new ActorDataItem[airSupply.HasValue ? 5 : 3];
        data[0] = new() { Id = (uint)ActorDataId.Reserved0, Type = DataItemType.Int64, Value = unchecked((long)(ulong)value) };
        data[1] = new() { Id = (uint)ActorDataId.AlwaysShowNameTag, Type = DataItemType.Byte, Value = (sbyte)(Get(ActorFlag.AlwaysShowName) ? 1 : 0) };
        data[2] = new() { Id = (uint)ActorDataId.Reserved092, Type = DataItemType.Int64, Value = unchecked((long)(ulong)(value >> 64)) };
        if (airSupply is short supply) {
            data[3] = new() { Id = (uint)ActorDataId.AirSupply, Type = DataItemType.Short, Value = supply };
            data[4] = new() { Id = (uint)ActorDataId.MaxAirSupply, Type = DataItemType.Short, Value = maximumAirSupply };
        }
        return data;
    }
}
