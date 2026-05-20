using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class StackResponseContainerInfo : DataType
{
    public FullContainerName Container { get; set; } = new();
    public List<StackResponseSlotInfo> SlotInfo { get; set; } = [];

    public void Read(BinaryReader reader)
    {
        Container.Read(reader);

        int count = reader.ReadVarInt();
        SlotInfo = new(count);
        for (int i = 0; i < count; i++)
        {
            StackResponseSlotInfo info = new();
            info.Read(reader);
            SlotInfo.Add(info);
        }
    }

    public void Write(BinaryWriter writer)
    {
        Container.Write(writer);

        writer.WriteVarInt(SlotInfo.Count);
        for (int i = 0; i < SlotInfo.Count; i++)
        {
            SlotInfo[i].Write(writer);
        }
    }
}
