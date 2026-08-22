using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class LegacySetSlot : DataType {
    public ContainerEnumName ContainerEnum;
    public byte[] Slots = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt8((byte)ContainerEnum);
        writer.WriteVarUInt((uint)Slots.Length);
        writer.WriteBytes(Slots);
    }

    public override void Read(ref BinaryReader reader) {
        ContainerEnum = (ContainerEnumName)reader.ReadUInt8();
        Slots = reader.ReadBytes(checked((int)reader.ReadVarUInt())).ToArray();
    }
}
