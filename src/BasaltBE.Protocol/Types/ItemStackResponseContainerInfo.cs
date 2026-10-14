using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ItemStackResponseContainerInfo : DataType {
    public FullContainerName ContainerName = new();
    public ItemStackResponseSlotInfo[] Slots = [];
    public override void Read(ref BinaryReader reader) {
        ContainerName.Read(ref reader);
        uint count = reader.ReadVarUInt();
        Slots = new ItemStackResponseSlotInfo[(int)count];
        for (int i = 0; i < Slots.Length; i++) { Slots[i] = new ItemStackResponseSlotInfo(); Slots[i].Read(ref reader); }
    }
    public override void Write(ref BinaryWriter writer) {
        ContainerName.Write(ref writer);
        writer.WriteVarUInt((uint)Slots.Length);
        foreach (ItemStackResponseSlotInfo slot in Slots) slot.Write(ref writer);
    }
}
