using Basalt.Binary;
using BasaltBE.Nbt;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using NbtCodec = BasaltBE.Nbt.Nbt;

namespace BasaltBE.Protocol.Types;

public sealed class ItemData : DataType {
    public string Name = string.Empty;
    public short Id;
    public bool ComponentBased;
    public ItemVersion Version;
    public CompoundTag ComponentData = new();

    public override void Read(ref BinaryReader reader) {
        Name = reader.ReadVarString();
        Id = reader.ReadInt16(true);
        ComponentBased = reader.ReadBool();
        Version = (ItemVersion)reader.ReadZigZag();
        Tag componentData = NbtCodec.Read(ref reader, new Options { VarInt = true, StringLengthVarInt = true });
        ComponentData = componentData as CompoundTag
            ?? throw new InvalidDataException("Item component data must be a compound tag.");
    }

    public override void Write(ref BinaryWriter writer) {
        ArgumentNullException.ThrowIfNull(ComponentData);
        writer.WriteVarString(Name);
        writer.WriteInt16(Id, true);
        writer.WriteBool(ComponentBased);
        writer.WriteZigZag((int)Version);
        NbtCodec.Write(ComponentData, ref writer, new Options { VarInt = true, StringLengthVarInt = true });
    }
}
