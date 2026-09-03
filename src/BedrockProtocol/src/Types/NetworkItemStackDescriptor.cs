using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class NetworkItemStackDescriptor : DataType {
    public short Id;
    public ushort StackSize;
    public uint AuxValue;
    public int? NetIdVariant;
    public uint BlockRuntimeId;
    public string UserDataBuffer = string.Empty;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteInt16(Id, true);
        writer.WriteUInt16(StackSize, true);
        writer.WriteVarUInt(AuxValue);
        writer.WriteBool(NetIdVariant is not null);

        if (NetIdVariant is int netIdVariant) {
            writer.WriteZigZong(netIdVariant);
        }

        writer.WriteVarUInt(BlockRuntimeId);
        if (Id == 0) {
            writer.WriteVarUInt(0);
        } else {
            byte[] userData = string.IsNullOrEmpty(UserDataBuffer)
                ? [0xFF, 0xFF, 0, 0]
                : Convert.FromBase64String(UserDataBuffer);
            writer.WriteVarUInt((uint)userData.Length);
            writer.WriteBytes(userData);
        }
    }

    public override void Read(ref BinaryReader reader) {
        Id = reader.ReadInt16(true);
        StackSize = reader.ReadUInt16(true);
        AuxValue = reader.ReadVarUInt();
        NetIdVariant = reader.ReadBool() ? checked((int)reader.ReadZigZong()) : null;
        BlockRuntimeId = reader.ReadVarUInt();
        int userDataLength = checked((int)reader.ReadVarUInt());
        UserDataBuffer = userDataLength == 0
            ? string.Empty
            : Convert.ToBase64String(reader.ReadBytes(userDataLength).ToArray());
    }
}
