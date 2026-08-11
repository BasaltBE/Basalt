using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SerializableCells {
    public byte XSize;
    public byte YSize;
    public byte ZSize;
    public List<byte> Storage = [];

    public void Read(BinaryReader reader) {
        XSize = reader.ReadUInt8();
        YSize = reader.ReadUInt8();
        ZSize = reader.ReadUInt8();
        int count6 = checked((int)reader.ReadVarUInt());
        Storage = new List<byte>(count6);
        for (int i6 = 0; i6 < count6; i6++) {
            byte item6 = default!;
            item6 = reader.ReadUInt8();
            Storage.Add(item6);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8(XSize);
        writer.WriteUInt8(YSize);
        writer.WriteUInt8(ZSize);
        writer.WriteVarUInt(checked((uint)Storage.Count));
        foreach (var item7 in Storage) {
            writer.WriteUInt8(item7);
        }
    }
}
