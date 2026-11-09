using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class AvailableCommandsConstrainedValueData : DataType {
    public uint EnumValueSymbol;
    public uint EnumSymbol;
    public byte[] ConstraintIndexes = [];

    public override void Read(ref BinaryReader reader) {
        EnumValueSymbol = reader.ReadUInt32(true);
        EnumSymbol = reader.ReadUInt32(true);
        ConstraintIndexes = new byte[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < ConstraintIndexes.Length; index++)
            ConstraintIndexes[index] = reader.ReadUInt8();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt32(EnumValueSymbol, true);
        writer.WriteUInt32(EnumSymbol, true);
        writer.WriteVarUInt((uint)ConstraintIndexes.Length);
        foreach (byte index in ConstraintIndexes)
            writer.WriteUInt8(index);
    }
}
