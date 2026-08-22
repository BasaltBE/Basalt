using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.NBT;
using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Nbt = Basalt.BedrockProtocol.NBT.NBT;

namespace Basalt.BedrockProtocol.Types;

public sealed class ActorDataItem : DataType {
    static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);
    public uint Id;
    public DataItemType Type;
    public object Value = (sbyte)0;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt(Id);
        writer.WriteVarUInt((uint)Type);
        writer.WriteUInt8((byte)Type);
        switch (Type) {
            case DataItemType.Byte: writer.WriteInt8(Convert.ToSByte(Value)); break;
            case DataItemType.Short: writer.WriteInt16(Convert.ToInt16(Value), true); break;
            case DataItemType.Int: writer.WriteZigZong(Convert.ToInt32(Value)); break;
            case DataItemType.Float: writer.WriteF32(Convert.ToSingle(Value), true); break;
            case DataItemType.String: writer.WriteVarString((string)Value); break;
            case DataItemType.CompoundTag: Nbt.WriteTag(writer, (CompoundTag)Value, NetworkNbtOptions); break;
            case DataItemType.Pos: ((BlockPos)Value).Write(ref writer); break;
            case DataItemType.Int64: writer.WriteZigZong(Convert.ToInt64(Value)); break;
            case DataItemType.Vec3: ((Vec3)Value).Write(ref writer); break;
            default: throw new ArgumentOutOfRangeException(nameof(Type));
        }
    }

    public override void Read(ref BinaryReader reader) {
        Id = reader.ReadVarUInt();
        uint type = reader.ReadVarUInt();
        byte legacyType = reader.ReadUInt8();
        if (type != legacyType)
            throw new FormatException("Actor data type selectors do not match.");

        Type = (DataItemType)type;
        Value = Type switch {
            DataItemType.Byte => reader.ReadInt8(),
            DataItemType.Short => reader.ReadInt16(true),
            DataItemType.Int => checked((int)reader.ReadZigZong()),
            DataItemType.Float => reader.ReadF32(true),
            DataItemType.String => reader.ReadVarString(),
            DataItemType.CompoundTag => Nbt.ReadTag<CompoundTag>(reader, NetworkNbtOptions),
            DataItemType.Pos => ReadBlockPosition(ref reader),
            DataItemType.Int64 => reader.ReadZigZong(),
            DataItemType.Vec3 => ReadVector(ref reader),
            _ => throw new FormatException("Unsupported actor data item type.")
        };
    }

    static BlockPos ReadBlockPosition(ref BinaryReader reader) {
        BlockPos value = new();
        value.Read(ref reader);
        return value;
    }

    static Vec3 ReadVector(ref BinaryReader reader) {
        Vec3 value = new();
        value.Read(ref reader);
        return value;
    }
}
