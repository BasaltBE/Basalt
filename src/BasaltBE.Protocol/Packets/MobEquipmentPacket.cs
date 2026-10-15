using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(31)]
public sealed class MobEquipmentPacket : DataPacket {
    public ulong TargetRuntimeId;
    public NetworkItemStackDescriptor Item = new();
    public byte Slot;
    public byte SelectedSlot;
    public byte ContainerId;

    public override void Deserialize(ref BinaryReader reader) {
        TargetRuntimeId = reader.ReadVarULong();
        Item.Read(ref reader);
        Slot = reader.ReadUInt8();
        SelectedSlot = reader.ReadUInt8();
        ContainerId = reader.ReadUInt8();
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarULong(TargetRuntimeId);
        Item.Write(ref writer);
        writer.WriteUInt8(Slot);
        writer.WriteUInt8(SelectedSlot);
        writer.WriteUInt8(ContainerId);
    }
}
