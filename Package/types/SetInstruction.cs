using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SetInstruction {
    public uint Preset;
    public EaseOption Ease = new();
    public PosOption Pos = new();
    public RotOption Rot = new();
    public FacingOption Facing = new();
    public ViewOffsetOption ViewOffset = new();
    public EntityOffsetOption EntityOffset = new();
    public bool Default;
    public bool RemoveIgnoreStartingValuesComponent;

    public void Read(BinaryReader reader) {
        Preset = reader.ReadUInt32(true);
        Ease.Read(reader);
        Pos.Read(reader);
        Rot.Read(reader);
        Facing.Read(reader);
        ViewOffset.Read(reader);
        EntityOffset.Read(reader);
        Default = reader.ReadBool();
        RemoveIgnoreStartingValuesComponent = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt32(Preset, true);
        Ease.Write(writer);
        Pos.Write(writer);
        Rot.Write(writer);
        Facing.Write(writer);
        ViewOffset.Write(writer);
        EntityOffset.Write(writer);
        writer.WriteBool(Default);
        writer.WriteBool(RemoveIgnoreStartingValuesComponent);
    }
}
