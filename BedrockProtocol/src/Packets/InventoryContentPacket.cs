using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(49)]
public sealed class InventoryContentPacket : DataPacket {
    public ContainerId ContainerId;
    public NetworkItemStackDescriptor[] Slots = [];
    public FullContainerName Container = new();
    public NetworkItemStackDescriptor StorageItem = new();

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)(sbyte)ContainerId);
        writer.WriteVarUInt((uint)Slots.Length);
        for (int i = 0; i < Slots.Length; i++) Slots[i].Write(ref writer);
        Container.Write(ref writer);
        StorageItem.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ContainerId = (ContainerId)reader.ReadVarUInt();
        int count = checked((int)reader.ReadVarUInt());
        Slots = new NetworkItemStackDescriptor[count];
        for (int i = 0; i < count; i++) {
            Slots[i] = new NetworkItemStackDescriptor();
            Slots[i].Read(ref reader);
        }
        Container.Read(ref reader);
        StorageItem.Read(ref reader);
    }
}
