using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ItemStackResponse)]
public sealed record ItemStackResponsePacket : DataPacket {
    /// <summary>
    /// Stack response entries.
    /// </summary>
    public List<ItemStackResponse> Responses = [];

    public override void Deserialize(Binary.BinaryReader reader) {
        int count = reader.ReadVarInt();
        Responses = new(count);
        for (int i = 0; i < count; i++) {
            ItemStackResponse response = new();
            response.Read(reader);
            Responses.Add(response);
        }
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteVarInt(Responses.Count);
        for (int i = 0; i < Responses.Count; i++) {
            Responses[i].Write(writer);
        }
    }
}
