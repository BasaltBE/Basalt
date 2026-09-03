using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class NetworkItemInstanceDescriptor : DataType {
    private static readonly byte[] EmptyUserData = [0xFF, 0xFF, 0, 0];

    public int Id;
    public ushort StackSize;
    public uint AuxValue;
    public int BlockRuntimeId;
    public string UserDataBuffer = string.Empty;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZag(Id);
        writer.WriteUInt16(StackSize, true);
        writer.WriteVarUInt(AuxValue);
        writer.WriteZigZag(BlockRuntimeId);
        if (Id == 0) {
            writer.WriteVarUInt(0);
        } else {
            byte[] userData = string.IsNullOrEmpty(UserDataBuffer)
                ? EmptyUserData
                : Convert.FromBase64String(UserDataBuffer);
            writer.WriteVarUInt((uint)userData.Length);
            writer.WriteBytes(userData);
        }
    }

    public override void Read(ref BinaryReader reader) {
        Id = reader.ReadZigZag();
        StackSize = reader.ReadUInt16(true);
        AuxValue = reader.ReadVarUInt();
        BlockRuntimeId = reader.ReadZigZag();
        int userDataLength = checked((int)reader.ReadVarUInt());
        UserDataBuffer = userDataLength == 0
            ? string.Empty
            : Convert.ToBase64String(reader.ReadBytes(userDataLength).ToArray());
    }

}
