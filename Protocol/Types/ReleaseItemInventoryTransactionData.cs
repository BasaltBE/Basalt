using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ReleaseItemInventoryTransactionData : IInventoryTransactionData {
    public InventoryTransactionType Type => InventoryTransactionType.ReleaseItem;

    /// <summary>
    /// Release-item action type.
    /// </summary>
    public int ActionType;
    /// <summary>
    /// Hotbar slot used by the client.
    /// </summary>
    public int HotBarSlot;
    /// <summary>
    /// Item held by the player.
    /// </summary>
    public NetworkItemStackDescriptor HeldItem = new();
    /// <summary>
    /// Head position at release time.
    /// </summary>
    public Vec3f HeadPosition;
    public void Read(BinaryReader reader) {
        ActionType = reader.ReadZigZag();
        HotBarSlot = reader.ReadZigZag();
        HeldItem.Read(reader);
        HeadPosition.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(ActionType);
        writer.WriteZigZag(HotBarSlot);
        HeldItem.Write(writer);
        HeadPosition.Write(writer);
    }
}
