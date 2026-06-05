using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ItemStackRequest)]
public sealed record ItemStackRequestPacket : DataPacket
{
    /// <summary>
    /// Stack request entries.
    /// </summary>
    public List<ItemStackRequest> Requests = [];

    public override void Deserialize(Binary.BinaryReader reader)
    {
        int count = checked((int)reader.ReadVarUInt());
        Requests = new(count);
        for (int i = 0; i < count; i++)
        {
            ItemStackRequest request = new();
            request.Read(reader);
            Requests.Add(request);
        }
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarUInt((uint)Requests.Count);
        for (int i = 0; i < Requests.Count; i++)
        {
            Requests[i].Write(writer);
        }
    }
}
