#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SerializedData {
    public uint AuxValue;
    public uint BlockRuntimeId;
    public short Id;
    public int? NetIdVariant;
    public ushort StackSize;
    public byte[] UserDataBuffer = [];

    public void Read(BinaryReader reader) {
        AuxValue = reader.ReadUInt32(true);
        BlockRuntimeId = reader.ReadUInt32(true);
        Id = reader.ReadInt16(true);
        if (reader.ReadBool()) {
            NetIdVariant = reader.ReadInt32(true);
        } else {
            NetIdVariant = default;
        }
        StackSize = reader.ReadUInt16(true);
        int binaryLength10 = checked((int)reader.ReadVarUInt());
        UserDataBuffer = reader.ReadBytes(binaryLength10).ToArray();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt32(AuxValue, true);
        writer.WriteUInt32(BlockRuntimeId, true);
        writer.WriteInt16(Id, true);
        writer.WriteBool(NetIdVariant is not null);
        if (NetIdVariant is { } optionalValue7) {
            writer.WriteInt32(optionalValue7, true);
        }
        writer.WriteUInt16(StackSize, true);
        writer.WriteVarUInt(checked((uint)UserDataBuffer.Length));
        writer.WriteBytes(UserDataBuffer);
    }
}
