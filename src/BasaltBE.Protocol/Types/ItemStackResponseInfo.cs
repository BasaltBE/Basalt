using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ItemStackResponseInfo : DataType {
    public ItemStackNetResult Result;
    public int ClientRequestId;
    public ItemStackResponseContainerInfo[]? Containers;
    public override void Read(ref BinaryReader reader) {
        Result = (ItemStackNetResult)reader.ReadUInt8();
        ClientRequestId = reader.ReadVarInt();
        if (reader.ReadBool()) {
            uint count = reader.ReadVarUInt();
            Containers = new ItemStackResponseContainerInfo[(int)count];
            for (int i = 0; i < Containers.Length; i++) { Containers[i] = new ItemStackResponseContainerInfo(); Containers[i].Read(ref reader); }
        }
    }
    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt8((byte)Result);
        writer.WriteVarInt(ClientRequestId);
        writer.WriteBool(Containers is not null);
        if (Containers is not null) {
            writer.WriteVarUInt((uint)Containers.Length);
            foreach (ItemStackResponseContainerInfo container in Containers) container.Write(ref writer);
        }
    }
}
