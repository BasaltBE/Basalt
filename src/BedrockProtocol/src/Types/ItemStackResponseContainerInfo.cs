using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ItemStackResponseContainerInfo : DataType {
    public FullContainerName Container = new();
    public ItemStackResponseSlotInfo[] Slots = [];

    public override void Write(ref BinaryWriter writer) {
        Container.Write(ref writer);
        writer.WriteVarUInt((uint)Slots.Length);
        for (int i = 0; i < Slots.Length; i++) Slots[i].Write(ref writer);
    }

    public override void Read(ref BinaryReader reader) {
        Container.Read(ref reader);
        int count = checked((int)reader.ReadVarUInt());
        Slots = new ItemStackResponseSlotInfo[count];
        for (int i = 0; i < count; i++) {
            Slots[i] = new ItemStackResponseSlotInfo();
            Slots[i].Read(ref reader);
        }
    }
}
