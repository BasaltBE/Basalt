using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.MobEquipment)]
public sealed record MobEquipmentPacket : DataPacket
{
    public ulong EntityRuntimeId;
    public NetworkItemStackDescriptor NewItem = new();
    public byte InventorySlot;
    public byte HotBarSlot;
    public byte WindowId;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        EntityRuntimeId = reader.ReadVarULong();
        NewItem.Read(reader);
        InventorySlot = reader.ReadUInt8();
        HotBarSlot = reader.ReadUInt8();
        WindowId = reader.ReadUInt8();
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarULong(EntityRuntimeId);
        NewItem.Write(writer);
        writer.WriteUInt8(InventorySlot);
        writer.WriteUInt8(HotBarSlot);
        writer.WriteUInt8(WindowId);
    }
}
