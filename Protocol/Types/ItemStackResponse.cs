using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ItemStackResponse : DataType
{
    public ItemStackResponseStatus Status { get; set; }
    public int RequestId { get; set; }
    public List<StackResponseContainerInfo> ContainerInfo { get; set; } = [];

    public void Read(ref BinaryReader reader)
    {
        Status = (ItemStackResponseStatus)reader.ReadUInt8();
        RequestId = reader.ReadZigZag();

        if (Status != ItemStackResponseStatus.Ok)
        {
            ContainerInfo = [];
            return;
        }

        int count = reader.ReadVarInt();
        ContainerInfo = new(count);
        for (int i = 0; i < count; i++)
        {
            StackResponseContainerInfo info = new();
            info.Read(ref reader);
            ContainerInfo.Add(info);
        }
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteUInt8((byte)Status);
        writer.WriteZigZag(RequestId);

        if (Status != ItemStackResponseStatus.Ok)
        {
            return;
        }

        writer.WriteVarInt(ContainerInfo.Count);
        for (int i = 0; i < ContainerInfo.Count; i++)
        {
            ContainerInfo[i].Write(ref writer);
        }
    }
}
