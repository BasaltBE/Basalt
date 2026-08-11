using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BedrockProtocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SynchedActorDataList {
    private static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);

    public List<DataItemEntry> Data = [];

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        Data = new List<DataItemEntry>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            DataItemEntry item0 = default!;
            DataItemEntry readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            Data.Add(item0);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)Data.Count));
        foreach (var item1 in Data) {
            item1.Write(writer);
        }
    }
}
