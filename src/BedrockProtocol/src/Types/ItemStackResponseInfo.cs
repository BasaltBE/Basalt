using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ItemStackResponseInfo : DataType {
    public byte Result;
    public int ClientRequestId;
    public ItemStackResponseContainerInfo[]? Containers;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt8(Result);
        writer.WriteVarInt(ClientRequestId);
        writer.WriteBool(Containers is not null);
        if (Containers is ItemStackResponseContainerInfo[] containers) {
            writer.WriteVarUInt((uint)containers.Length);
            for (int i = 0; i < containers.Length; i++) containers[i].Write(ref writer);
        }
    }

    public override void Read(ref BinaryReader reader) {
        Result = reader.ReadUInt8();
        ClientRequestId = reader.ReadVarInt();
        if (!reader.ReadBool()) {
            Containers = null;
            return;
        }

        int count = checked((int)reader.ReadVarUInt());
        Containers = new ItemStackResponseContainerInfo[count];
        for (int i = 0; i < count; i++) {
            Containers[i] = new ItemStackResponseContainerInfo();
            Containers[i].Read(ref reader);
        }
    }
}
