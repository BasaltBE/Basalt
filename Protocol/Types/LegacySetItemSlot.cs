using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class LegacySetItemSlot : DataType
{
    /// <summary>
    /// Legacy container name identifier.
    /// </summary>
    public byte ContainerId;
    /// <summary>
    /// Legacy slot byte list.
    /// </summary>
    public byte[] Slots = [];

    public void Read(BinaryReader reader)
    {
        ContainerId = reader.ReadUInt8();
        Slots = reader.ReadBytes(checked((int)reader.ReadVarUInt())).ToArray();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteUInt8(ContainerId);
        writer.WriteVarUInt((uint)Slots.Length);
        writer.WriteBytes(Slots);
    }
}
