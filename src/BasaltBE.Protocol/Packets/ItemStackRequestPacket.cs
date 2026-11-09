using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(147)]
public sealed class ItemStackRequestPacket : DataPacket {
    public ItemStackRequestData[] Requests = [];

    public override void Serialize(ref BinaryWriter writer) {
        if (Requests.Length > 100) throw new InvalidDataException("Item stack request packet cannot contain more than 100 requests.");
        writer.WriteVarUInt((uint)Requests.Length);
        foreach (ItemStackRequestData request in Requests) request.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        uint count = reader.ReadVarUInt();
        if (count > 100) throw new InvalidDataException("Item stack request packet cannot contain more than 100 requests.");
        Requests = new ItemStackRequestData[(int)count];
        for (int i = 0; i < Requests.Length; i++) {
            Requests[i] = new ItemStackRequestData();
            Requests[i].Read(ref reader);
        }
    }
}
