using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class StackResponseContainerInfo : DataType
{
    public FullContainerName Container { get; set; } = new();
    public List<StackResponseSlotInfo> SlotInfo { get; set; } = [];

    public void Read(ref BinaryReader reader)
    {
        Container.Read(ref reader);

        int count = checked((int)reader.ReadVarUInt());
        SlotInfo = new(count);
        for (int i = 0; i < count; i++)
        {
            StackResponseSlotInfo info = new();
            info.Read(ref reader);
            SlotInfo.Add(info);
        }
    }

    public void Write(ref BinaryWriter writer)
    {
        Container.Write(ref writer);

        writer.WriteVarUInt((uint)SlotInfo.Count);
        for (int i = 0; i < SlotInfo.Count; i++)
        {
            SlotInfo[i].Write(ref writer);
        }
    }
}
