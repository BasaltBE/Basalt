using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class InventorySourceData : DataType {
    public InventorySourceType Type;
    public ContainerId? ContainerId;
    public InventorySourceFlags? Flags;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Type);
        writer.WriteBool(ContainerId.HasValue);
        if (ContainerId is ContainerId containerId) writer.WriteInt8((sbyte)containerId);
        writer.WriteBool(Flags.HasValue);
        if (Flags.HasValue) writer.WriteVarUInt((uint)Flags.Value);
    }

    public override void Read(ref BinaryReader reader) {
        Type = (InventorySourceType)reader.ReadVarUInt();
        ContainerId = reader.ReadBool() ? (ContainerId)reader.ReadInt8() : null;
        Flags = reader.ReadBool() ? (InventorySourceFlags)reader.ReadVarUInt() : null;
    }
}
