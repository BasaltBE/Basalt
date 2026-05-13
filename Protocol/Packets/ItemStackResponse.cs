using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record ItemStackResponsePacket : DataPacket
{
    public List<ItemStackResponse> Responses { get; set; } = [];

    public override PacketId PacketId => PacketId.ItemStackResponse;

    public override void Deserialize(ref BinaryReader reader)
    {
        int count = reader.ReadVarInt();
        Responses = new(count);
        for (int i = 0; i < count; i++)
        {
            ItemStackResponse response = new();
            response.Read(ref reader);
            Responses.Add(response);
        }
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteVarInt(Responses.Count);
        for (int i = 0; i < Responses.Count; i++)
        {
            Responses[i].Write(ref writer);
        }
    }
}
