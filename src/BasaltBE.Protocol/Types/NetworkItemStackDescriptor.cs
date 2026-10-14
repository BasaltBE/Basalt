using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class NetworkItemStackDescriptor : DataType {
    public short Id;
    public ushort StackSize;
    public uint AuxValue;
    public int? NetIdVariant;
    public uint BlockRuntimeId;
    public string UserData = string.Empty;

    public override void Read(ref BinaryReader reader) {
        Id = reader.ReadInt16(true);
        StackSize = reader.ReadUInt16(true);
        AuxValue = reader.ReadVarUInt();
        NetIdVariant = reader.ReadBool() ? reader.ReadVarInt() : null;
        BlockRuntimeId = reader.ReadVarUInt();
        UserData = reader.ReadVarString();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteInt16(Id, true);
        writer.WriteUInt16(StackSize, true);
        writer.WriteVarUInt(AuxValue);
        writer.WriteBool(NetIdVariant.HasValue);
        if (NetIdVariant is int netIdVariant) writer.WriteVarInt(netIdVariant);
        writer.WriteVarUInt(BlockRuntimeId);
        writer.WriteVarString(UserData);
    }
}
