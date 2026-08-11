using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackResponseInfo {
    public ItemStackNetResult Result;
    public int ClientRequestId;
    public List<ItemStackResponseContainerInfo>? Containers;

    public void Read(BinaryReader reader) {
        Result = (global::BedrockProtocol.Enums.ItemStackNetResult)reader.ReadUInt8();
        ClientRequestId = reader.ReadZigZag();
        if (reader.ReadBool()) {
            if (reader.ReadBool()) {
                int count4 = checked((int)reader.ReadVarUInt());
                Containers = new List<ItemStackResponseContainerInfo>(count4);
                for (int i4 = 0; i4 < count4; i4++) {
                    ItemStackResponseContainerInfo item4 = default!;
                    ItemStackResponseContainerInfo readValue1004 = new();
                    readValue1004.Read(reader);
                    item4 = readValue1004;
                    Containers.Add(item4);
                }
            } else {
                Containers = default;
            }
        } else {
            Containers = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)Result);
        writer.WriteZigZag(ClientRequestId);
        writer.WriteBool(Containers is not null);
        if (Containers is not null) {
            writer.WriteBool(Containers is not null);
            if (Containers is { } optionalValue5) {
                writer.WriteVarUInt(checked((uint)optionalValue5.Count));
                foreach (var item5 in optionalValue5) {
                    item5.Write(writer);
                }
            }
        }
    }
}
