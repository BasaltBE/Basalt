using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CreativeItemNetId {
    public uint ID;

    public void Read(BinaryReader reader) {
        ID = reader.ReadVarUInt();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(ID);
    }
}
