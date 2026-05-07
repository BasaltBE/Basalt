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
        int count = checked((int)reader.ReadVarUInt());
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
        writer.WriteVarUInt((uint)Responses.Count);
        for (int i = 0; i < Responses.Count; i++)
        {
            Responses[i].Write(ref writer);
        }
    }
}
