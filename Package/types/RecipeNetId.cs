#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class RecipeNetId {
    public uint RawId;

    public void Read(BinaryReader reader) {
        RawId = reader.ReadVarUInt();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(RawId);
    }
}
