#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class MemoryCategoryCounter {
    public MemoryCategory Category;
    public ulong CurrentBytes;

    public void Read(BinaryReader reader) {
        Category = (global::BedrockProtocol.Enums.MemoryCategory)reader.ReadUInt8();
        CurrentBytes = reader.ReadUInt64(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)Category);
        writer.WriteUInt64(CurrentBytes, true);
    }
}
