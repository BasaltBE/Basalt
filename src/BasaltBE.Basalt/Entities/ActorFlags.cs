using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Entities;

public sealed class ActorFlags {
    private static readonly (string Name, ActorFlag Flag)[] persistentFlags = [
        ("IsBaby", ActorFlag.Baby),
        ("IsAngry", ActorFlag.Angry),
        ("IsTamed", ActorFlag.Tamed),
        ("IsOrphaned", ActorFlag.Orphaned),
        ("IsIllagerCaptain", ActorFlag.IllagerCaptain),
        ("IsTrusting", ActorFlag.Trusting),
        ("Saddled", ActorFlag.Saddled),
        ("Sheared", ActorFlag.Sheared),
        ("Chested", ActorFlag.Chested),
        ("ShowBottom", ActorFlag.ShowBottom),
        ("Sitting", ActorFlag.Sitting),
        ("CustomNameVisible", ActorFlag.AlwaysShowName)
    ];

    private UInt128 value;
    short? airSupply;
    short maximumAirSupply;
    internal float? CollisionWidth;
    internal float CollisionHeight;
    public bool Dirty;

    internal void Read(CompoundTag tag) {
        foreach ((string name, ActorFlag flag) in persistentFlags) {
            if (tag.Get<ByteTag>(name) is { } saved) Set(flag, saved.Value != 0);
        }
    }

    internal void Write(CompoundTag tag) {
        foreach ((string name, ActorFlag flag) in persistentFlags)
            tag.Add(name, new ByteTag { Value = Get(flag) ? (byte)1 : (byte)0 });
    }

    internal void SetAirSupply(short supply, short maximum) {
        Dirty |= airSupply != supply || maximumAirSupply != maximum;
        airSupply = supply;
        maximumAirSupply = maximum;
    }

    public bool Get(ActorFlag flag) {
        int index = (int)flag;
        if ((uint)index >= 127) throw new ArgumentOutOfRangeException(nameof(flag));
        return (value & ((UInt128)1 << index)) != 0;
    }

    internal bool Set(ActorFlag flag, bool enabled) {
        int index = (int)flag;
        if ((uint)index >= 127) throw new ArgumentOutOfRangeException(nameof(flag));

        UInt128 mask = (UInt128)1 << index;
        bool changed = ((value & mask) != 0) != enabled;
        if (enabled) value |= mask;
        else value &= ~mask;
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
        int length = airSupply.HasValue ? 5 : 3;
        ActorDataItem[] data = new ActorDataItem[length + (CollisionWidth.HasValue ? 2 : 0)];
        data[0] = new() { Id = (uint)ActorDataId.Reserved0, Type = DataItemType.Int64, Value = unchecked((long)(ulong)value) };
        data[1] = new() { Id = (uint)ActorDataId.AlwaysShowNameTag, Type = DataItemType.Byte, Value = (sbyte)(Get(ActorFlag.AlwaysShowName) ? 1 : 0) };
        data[2] = new() { Id = (uint)ActorDataId.Reserved092, Type = DataItemType.Int64, Value = unchecked((long)(ulong)(value >> 64)) };
        if (airSupply is short supply) {
            data[3] = new() { Id = (uint)ActorDataId.AirSupply, Type = DataItemType.Short, Value = supply };
            data[4] = new() { Id = (uint)ActorDataId.MaxAirSupply, Type = DataItemType.Short, Value = maximumAirSupply };
        }
        if (CollisionWidth is float width) {
            data[length] = new() { Id = (uint)ActorDataId.BoundingBoxWidth, Type = DataItemType.Float, Value = width };
            data[length + 1] = new() { Id = (uint)ActorDataId.BoundingBoxHeight, Type = DataItemType.Float, Value = CollisionHeight };
        }

        return data;
    }
}
