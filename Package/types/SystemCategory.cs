#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SystemCategory {
    public string CategoryName = string.Empty;
    public ulong SystemIndex;

    public void Read(BinaryReader reader) {
        CategoryName = reader.ReadVarString();
        SystemIndex = reader.ReadUInt64(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(CategoryName);
        writer.WriteUInt64(SystemIndex, true);
    }
}
