#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class NetworkItemInstanceDescriptorData {
    public int Id;
    public ushort StackSize;
    public uint AuxValue;
    public int BlockRuntimeId;
    public byte[] UserDataBuffer = [];

    public void Read(BinaryReader reader) {
        Id = reader.ReadZigZag();
        StackSize = reader.ReadUInt16(true);
        AuxValue = reader.ReadVarUInt();
        BlockRuntimeId = reader.ReadZigZag();
        int binaryLength8 = checked((int)reader.ReadVarUInt());
        UserDataBuffer = reader.ReadBytes(binaryLength8).ToArray();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(Id);
        writer.WriteUInt16(StackSize, true);
        writer.WriteVarUInt(AuxValue);
        writer.WriteZigZag(BlockRuntimeId);
        writer.WriteVarUInt(checked((uint)UserDataBuffer.Length));
        writer.WriteBytes(UserDataBuffer);
    }
}
