using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(31)]
public sealed class MobEquipmentPacket : DataPacket {
    public ulong TargetRuntimeId;
    public NetworkItemStackDescriptor Item = new();
    public byte Slot;
    public byte SelectedSlot;
    public ContainerId ContainerId;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarULong(TargetRuntimeId);
        Item.Write(ref writer);
        writer.WriteUInt8(Slot);
        writer.WriteUInt8(SelectedSlot);
        writer.WriteUInt8((byte)ContainerId);
    }

    public override void Deserialize(ref BinaryReader reader) {
        TargetRuntimeId = reader.ReadVarULong();
        Item.Read(ref reader);
        Slot = reader.ReadUInt8();
        SelectedSlot = reader.ReadUInt8();
        ContainerId = (ContainerId)reader.ReadUInt8();
    }
}
