#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class NetworkItemStackDescriptor {
    public short Id;
    public ushort StackSize;
    public uint AuxValue;
    public int? NetIdVariant;
    public uint BlockRuntimeId;
    public byte[] UserDataBuffer = [];

    public void Read(BinaryReader reader) {
        Id = reader.ReadInt16(true);
        StackSize = reader.ReadUInt16(true);
        AuxValue = reader.ReadVarUInt();
        if (reader.ReadBool()) {
            NetIdVariant = reader.ReadZigZag();
        } else {
            NetIdVariant = default;
        }
        BlockRuntimeId = reader.ReadVarUInt();
        int binaryLength10 = checked((int)reader.ReadVarUInt());
        UserDataBuffer = reader.ReadBytes(binaryLength10).ToArray();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt16(Id, true);
        writer.WriteUInt16(StackSize, true);
        writer.WriteVarUInt(AuxValue);
        writer.WriteBool(NetIdVariant is not null);
        if (NetIdVariant is { } optionalValue7) {
            writer.WriteZigZag(optionalValue7);
        }
        writer.WriteVarUInt(BlockRuntimeId);
        writer.WriteVarUInt(checked((uint)UserDataBuffer.Length));
        writer.WriteBytes(UserDataBuffer);
    }
}
