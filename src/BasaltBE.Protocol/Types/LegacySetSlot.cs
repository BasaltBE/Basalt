using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class LegacySetSlot : DataType {
    public ContainerEnumName ContainerEnum;
    public byte[] Slots = [];

    public override void Read(ref BinaryReader reader) {
        ContainerEnum = (ContainerEnumName)reader.ReadUInt8();
        uint count = reader.ReadVarUInt();
        if (count > reader.Remaining)
            throw new InvalidDataException("Legacy slot count exceeds the remaining payload.");
        Slots = new byte[(int)count];
        for (int i = 0; i < Slots.Length; i++) Slots[i] = reader.ReadUInt8();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt8((byte)ContainerEnum);
        writer.WriteVarUInt((uint)Slots.Length);
        foreach (byte slot in Slots) writer.WriteUInt8(slot);
    }
}
