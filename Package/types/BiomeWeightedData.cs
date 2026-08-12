#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeWeightedData {
    public ushort BiomeIdentifier;
    public uint Weight;

    public void Read(BinaryReader reader) {
        BiomeIdentifier = reader.ReadUInt16(true);
        Weight = reader.ReadUInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt16(BiomeIdentifier, true);
        writer.WriteUInt32(Weight, true);
    }
}
