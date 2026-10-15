using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class NetworkItemStackDescriptor : DataType {
    static readonly byte[] EmptyUserData = new byte[10];

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
        NetIdVariant = reader.ReadBool() ? checked((int)reader.ReadZigZong()) : null;
        BlockRuntimeId = reader.ReadVarUInt();
        uint userDataLength = reader.ReadVarUInt();
        UserData = userDataLength == 0
            ? string.Empty
            : Convert.ToBase64String(reader.ReadBytes(checked((int)userDataLength)).ToArray());
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteInt16(Id, true);
        writer.WriteUInt16(StackSize, true);
        writer.WriteVarUInt(AuxValue);
        writer.WriteBool(NetIdVariant.HasValue);
        if (NetIdVariant is int netIdVariant) writer.WriteZigZong(netIdVariant);
        writer.WriteVarUInt(BlockRuntimeId);
        byte[] userData = Id == 0
            ? []
            : string.IsNullOrEmpty(UserData)
                ? EmptyUserData
                : Convert.FromBase64String(UserData);
        writer.WriteVarUInt((uint)userData.Length);
        writer.WriteBytes(userData);
    }
}
