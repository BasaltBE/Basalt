using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class InventoryAction : DataType {
    /// <summary>
    /// Source type of this action.
    /// </summary>
    public uint SourceType;
    /// <summary>
    /// Window id for container sources.
    /// </summary>
    public sbyte WindowId;
    /// <summary>
    /// Source flags for world sources.
    /// </summary>
    public uint SourceFlags;
    /// <summary>
    /// Slot index affected by the action.
    /// </summary>
    public uint InventorySlot;
    /// <summary>
    /// Item state before the action.
    /// </summary>
    public NetworkItemStackDescriptor OldItem = new();
    /// <summary>
    /// Item state after the action.
    /// </summary>
    public NetworkItemStackDescriptor NewItem = new();

    public void Read(BinaryReader reader) {
        SourceType = reader.ReadVarUInt();
        _ = reader.ReadBool();
        bool hasContainerId = reader.ReadBool();
        if (hasContainerId) {
            WindowId = reader.ReadInt8();
        }

        _ = reader.ReadBool();
        bool hasFlags = reader.ReadBool();
        if (hasFlags) {
            SourceFlags = reader.ReadVarUInt();
        }

        InventorySlot = reader.ReadVarUInt();
        OldItem.Read(reader);
        NewItem.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(SourceType);
        writer.WriteBool(true);
        bool hasContainerId = SourceType == 0 || SourceType == 99999;
        writer.WriteBool(hasContainerId);
        if (hasContainerId) {
            writer.WriteInt8(WindowId);
        }

        writer.WriteBool(true);
        bool hasFlags = SourceType == 2;
        writer.WriteBool(hasFlags);
        if (hasFlags) {
            writer.WriteVarUInt(SourceFlags);
        }

        writer.WriteVarUInt(InventorySlot);
        OldItem.Write(writer);
        NewItem.Write(writer);
    }
}
