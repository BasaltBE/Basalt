using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class LegacySetItemSlot : DataType
{
    public byte ContainerId { get; set; }
    public byte[] Slots { get; set; } = [];

    public void Read(ref BinaryReader reader)
    {
        ContainerId = reader.ReadUInt8();
        Slots = reader.ReadBytes(checked((int)reader.ReadVarUInt())).ToArray();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteUInt8(ContainerId);
        writer.WriteVarUInt((uint)Slots.Length);
        writer.WriteBytes(Slots);
    }
}
