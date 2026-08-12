#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ActorDataFlagComponent {
    public List<byte> ActorFlagBitsetData = [];

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        ActorFlagBitsetData = new List<byte>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            byte item0 = default!;
            item0 = reader.ReadUInt8();
            ActorFlagBitsetData.Add(item0);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)ActorFlagBitsetData.Count));
        foreach (var item1 in ActorFlagBitsetData) {
            writer.WriteUInt8(item1);
        }
    }
}
