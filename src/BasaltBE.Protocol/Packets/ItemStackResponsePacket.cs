using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(148)]
public sealed class ItemStackResponsePacket : DataPacket {
    public ItemStackResponseInfo[] Responses = [];

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Responses.Length);
        foreach (ItemStackResponseInfo response in Responses) response.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        uint count = reader.ReadVarUInt();
        Responses = new ItemStackResponseInfo[(int)count];
        for (int i = 0; i < Responses.Length; i++) {
            Responses[i] = new ItemStackResponseInfo();
            Responses[i].Read(ref reader);
        }
    }
}
