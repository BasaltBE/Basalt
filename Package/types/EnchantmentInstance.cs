#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class EnchantmentInstance {
    public EnchantType EnchantType = new();
    public byte EnchantLevel;

    public void Read(BinaryReader reader) {
        EnchantType = (EnchantType)reader.ReadUInt8();
        EnchantLevel = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)EnchantType);
        writer.WriteUInt8(EnchantLevel);
    }
}
