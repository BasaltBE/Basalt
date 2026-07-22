using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ItemStackResponse : DataType {
    /// <summary>
    /// Response status for the request.
    /// </summary>
    public ItemStackResponseStatus Status;
    /// <summary>
    /// Request id this response belongs to.
    /// </summary>
    public int RequestId;
    /// <summary>
    /// Updated container slot state when successful.
    /// </summary>
    public List<StackResponseContainerInfo> ContainerInfo = [];

    public void Read(BinaryReader reader) {
        Status = (ItemStackResponseStatus)reader.ReadUInt8();
        RequestId = reader.ReadZigZag();

        if (Status != ItemStackResponseStatus.Ok) {
            ContainerInfo = [];
            return;
        }

        int count = reader.ReadVarInt();
        ContainerInfo = new(count);
        for (int i = 0; i < count; i++) {
            StackResponseContainerInfo info = new();
            info.Read(reader);
            ContainerInfo.Add(info);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)Status);
        writer.WriteZigZag(RequestId);

        if (Status != ItemStackResponseStatus.Ok) {
            return;
        }

        writer.WriteVarInt(ContainerInfo.Count);
        for (int i = 0; i < ContainerInfo.Count; i++) {
            ContainerInfo[i].Write(writer);
        }
    }
}
