#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class StructureSettings {
    public string StructurePaletteName = string.Empty;
    public bool ShouldIgnoreEntities;
    public bool ShouldIgnoreBlocks;
    public bool ShouldAllowNonTickingPlayerAndTickingAreaChunks;
    public BlockPos StructureSize = new();
    public BlockPos StructureOffset = new();
    public ActorUniqueID LastEditPlayer = new();
    public Rotation Rotation;
    public Mirror Mirror;
    public AnimationMode AnimationMode;
    public float AnimationSeconds;
    public float IntegrityValue;
    public uint IntegritySeed;
    public Vec3 RotationPivot = new();

    public void Read(BinaryReader reader) {
        StructurePaletteName = reader.ReadVarString();
        ShouldIgnoreEntities = reader.ReadBool();
        ShouldIgnoreBlocks = reader.ReadBool();
        ShouldAllowNonTickingPlayerAndTickingAreaChunks = reader.ReadBool();
        StructureSize.Read(reader);
        StructureOffset.Read(reader);
        LastEditPlayer.Read(reader);
        Rotation = (global::BedrockProtocol.Enums.Rotation)reader.ReadUInt8();
        Mirror = (global::BedrockProtocol.Enums.Mirror)reader.ReadUInt8();
        AnimationMode = (global::BedrockProtocol.Enums.AnimationMode)reader.ReadUInt8();
        AnimationSeconds = reader.ReadF32(true);
        IntegrityValue = reader.ReadF32(true);
        IntegritySeed = reader.ReadUInt32(true);
        RotationPivot.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(StructurePaletteName);
        writer.WriteBool(ShouldIgnoreEntities);
        writer.WriteBool(ShouldIgnoreBlocks);
        writer.WriteBool(ShouldAllowNonTickingPlayerAndTickingAreaChunks);
        StructureSize.Write(writer);
        StructureOffset.Write(writer);
        LastEditPlayer.Write(writer);
        writer.WriteUInt8((byte)Rotation);
        writer.WriteUInt8((byte)Mirror);
        writer.WriteUInt8((byte)AnimationMode);
        writer.WriteF32(AnimationSeconds, true);
        writer.WriteF32(IntegrityValue, true);
        writer.WriteUInt32(IntegritySeed, true);
        RotationPivot.Write(writer);
    }
}
