using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(147)]
public sealed class ItemStackRequestPacket : DataPacket {
    public ItemStackRequestData[] Requests = [];

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Requests.Length);
        foreach (ItemStackRequestData request in Requests) request.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Requests = new ItemStackRequestData[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < Requests.Length; index++) {
            ItemStackRequestData request = new();
            request.Read(ref reader);
            Requests[index] = request;
        }
    }
}
