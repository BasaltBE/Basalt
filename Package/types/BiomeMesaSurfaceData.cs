using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeMesaSurfaceData {
    public uint ClayMaterial;
    public uint HardClayMaterial;
    public bool BrycePillars;
    public bool HasForest;

    public void Read(BinaryReader reader) {
        ClayMaterial = reader.ReadUInt32(true);
        HardClayMaterial = reader.ReadUInt32(true);
        BrycePillars = reader.ReadBool();
        HasForest = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt32(ClayMaterial, true);
        writer.WriteUInt32(HardClayMaterial, true);
        writer.WriteBool(BrycePillars);
        writer.WriteBool(HasForest);
    }
}
