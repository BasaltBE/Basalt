using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record ItemStackRequestPacket : DataPacket
{
    public List<ItemStackRequest> Requests { get; set; } = [];

    public override PacketId PacketId => PacketId.ItemStackRequest;

    public override void Deserialize(BinaryReader reader)
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

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarUInt((uint)Requests.Count);
        for (int i = 0; i < Requests.Count; i++)
        {
            Requests[i].Write(writer);
        }
    }
}
