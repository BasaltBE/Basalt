using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class InventorySource : DataType {
    public InventorySourceType Type;
    public sbyte? ContainerId;
    public InventorySourceFlags? Flags;

    public override void Read(ref BinaryReader reader) {
        Type = (InventorySourceType)reader.ReadVarUInt();
        ContainerId = reader.ReadBool() ? reader.ReadInt8() : null;
        Flags = reader.ReadBool() ? (InventorySourceFlags)reader.ReadVarUInt() : null;
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Type);
        writer.WriteBool(ContainerId.HasValue);
        if (ContainerId is sbyte containerId) writer.WriteInt8(containerId);
        writer.WriteBool(Flags.HasValue);
        if (Flags is InventorySourceFlags flags) writer.WriteVarUInt((uint)flags);
    }
}
