#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeSurfaceBuilderData {
    public BiomeSurfaceMaterialData SurfaceMaterials = new();
    public bool HasDefaultOverworldSurface;
    public bool HasSwampSurface;
    public bool HasFrozenOceanSurface;
    public bool HasTheEndSurface;
    public BiomeMesaSurfaceData MesaSurface = new();
    public BiomeCappedSurfaceData CappedSurface = new();
    public BiomeNoiseGradientSurfaceData NoiseGradientSurface = new();

    public void Read(BinaryReader reader) {
        SurfaceMaterials.Read(reader);
        HasDefaultOverworldSurface = reader.ReadBool();
        HasSwampSurface = reader.ReadBool();
        HasFrozenOceanSurface = reader.ReadBool();
        HasTheEndSurface = reader.ReadBool();
        MesaSurface.Read(reader);
        CappedSurface.Read(reader);
        NoiseGradientSurface.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        SurfaceMaterials.Write(writer);
        writer.WriteBool(HasDefaultOverworldSurface);
        writer.WriteBool(HasSwampSurface);
        writer.WriteBool(HasFrozenOceanSurface);
        writer.WriteBool(HasTheEndSurface);
        MesaSurface.Write(writer);
        CappedSurface.Write(writer);
        NoiseGradientSurface.Write(writer);
    }
}
