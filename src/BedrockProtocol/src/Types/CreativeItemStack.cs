using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class CreativeItemStack : DataType {
    private static readonly byte[] EmptyUserData = [0xFF, 0xFF, 0, 0];

    public int Id;
    public ushort Count;
    public uint Metadata;
    public int BlockRuntimeId;
    public string UserDataBuffer = string.Empty;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZong(Id);
        writer.WriteUInt16(Count, true);
        writer.WriteVarUInt(Metadata);
        writer.WriteZigZong(BlockRuntimeId);

        if (Id == 0) {
            writer.WriteVarUInt(0);
            return;
        }

        byte[] userData = string.IsNullOrEmpty(UserDataBuffer)
            ? EmptyUserData
            : Convert.FromBase64String(UserDataBuffer);
        writer.WriteVarUInt((uint)userData.Length);
        writer.WriteBytes(userData);
    }

    public override void Read(ref BinaryReader reader) {
        Id = checked((int)reader.ReadZigZong());
        Count = reader.ReadUInt16(true);
        Metadata = reader.ReadVarUInt();
        BlockRuntimeId = checked((int)reader.ReadZigZong());
        int userDataLength = checked((int)reader.ReadVarUInt());
        UserDataBuffer = userDataLength == 0
            ? string.Empty
            : Convert.ToBase64String(reader.ReadBytes(userDataLength).ToArray());
    }
}
