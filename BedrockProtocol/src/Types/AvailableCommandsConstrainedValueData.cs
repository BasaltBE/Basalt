using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class AvailableCommandsConstrainedValueData : DataType {
    public uint EnumValueSymbol;
    public uint EnumSymbol;
    public byte[] ConstraintIndices = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt32(EnumValueSymbol, true);
        writer.WriteUInt32(EnumSymbol, true);
        writer.WriteVarUInt((uint)ConstraintIndices.Length);
        for (int i = 0; i < ConstraintIndices.Length; i++) writer.WriteUInt8(ConstraintIndices[i]);
    }

    public override void Read(ref BinaryReader reader) {
        EnumValueSymbol = reader.ReadUInt32(true);
        EnumSymbol = reader.ReadUInt32(true);
        int count = checked((int)reader.ReadVarUInt());
        ConstraintIndices = new byte[count];
        for (int i = 0; i < count; i++) ConstraintIndices[i] = reader.ReadUInt8();
    }
}
