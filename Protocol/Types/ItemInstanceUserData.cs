using Basalt.Protocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ItemInstanceUserData : DataType<int?> {
    private const short NbtMarker = -1;
    private const byte NbtVersion = 1;

    /// <summary>
    /// Optional NBT payload.
    /// </summary>
    public CompoundTag? Nbt;

    /// <summary>
    /// Blocks the item can be placed on.
    /// </summary>
    public List<string> CanPlaceOn = [];

    /// <summary>
    /// Blocks the item can destroy.
    /// </summary>
    public List<string> CanDestroy = [];

    /// <summary>
    /// Optional ticking value for shield items.
    /// </summary>
    public long? Ticking;

    public void Read(BinaryReader reader) {
        Read(reader, null);
    }

    public void Write(BinaryWriter writer) {
        Write(writer, null);
    }

    public void Read(BinaryReader reader, int? networkId) {
        short marker = reader.ReadInt16(true);
        if (marker == NbtMarker) {
            byte version = reader.ReadUInt8();
            if (version != NbtVersion) {
                throw new InvalidOperationException($"Unsupported item NBT formatting version: {version}");
            }

            Nbt = Io.NBT.ReadTag<CompoundTag>(reader, new Nbt.TagOptions(Name: true, Type: true, VarInt: false));
        }
        else if (marker > 0) {
            Nbt = Io.NBT.ReadTag<CompoundTag>(reader, new Nbt.TagOptions(Name: true, Type: true, VarInt: false));
        }
        else {
            Nbt = null;
        }

        int canPlaceOnCount = checked((int)reader.ReadUInt32(true));
        CanPlaceOn = new(canPlaceOnCount);
        for (int i = 0; i < canPlaceOnCount; i++) {
            CanPlaceOn.Add(reader.ReadString16(true));
        }

        int canDestroyCount = checked((int)reader.ReadUInt32(true));
        CanDestroy = new(canDestroyCount);
        for (int i = 0; i < canDestroyCount; i++) {
            CanDestroy.Add(reader.ReadString16(true));
        }

        if (networkId == Io.Constants.ShieldNetworkId) {
            Ticking = reader.Remaining >= sizeof(long) ? reader.ReadInt64(true) : null;
        }
        else {
            Ticking = null;
        }
    }

    public void Write(BinaryWriter writer, int? networkId) {
        if (Nbt is null) {
            writer.WriteInt16(0, true);
        }
        else {
            writer.WriteInt16(NbtMarker, true);
            writer.WriteUInt8(NbtVersion);
            Io.NBT.WriteTag(writer, Nbt, new Nbt.TagOptions(Name: true, Type: true, VarInt: false));
        }

        writer.WriteUInt32(checked((uint)CanPlaceOn.Count), true);
        for (int i = 0; i < CanPlaceOn.Count; i++) {
            writer.WriteString16(CanPlaceOn[i], true);
        }

        writer.WriteUInt32(checked((uint)CanDestroy.Count), true);
        for (int i = 0; i < CanDestroy.Count; i++) {
            writer.WriteString16(CanDestroy[i], true);
        }

        if (networkId == Io.Constants.ShieldNetworkId) {
            writer.WriteInt64(Ticking ?? 0, true);
        }
    }
}
