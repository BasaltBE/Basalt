using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SerializedAbilitiesDataSerializedLayer {
    public ushort SerializedLayer;
    public uint AbilitiesSet;
    public uint AbilityValues;
    public float FlySpeed;
    public float VerticalFlySpeed;
    public float WalkSpeed;

    public void Read(BinaryReader reader) {
        SerializedLayer = reader.ReadUInt16(true);
        AbilitiesSet = reader.ReadUInt32(true);
        AbilityValues = reader.ReadUInt32(true);
        FlySpeed = reader.ReadF32(true);
        VerticalFlySpeed = reader.ReadF32(true);
        WalkSpeed = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt16(SerializedLayer, true);
        writer.WriteUInt32(AbilitiesSet, true);
        writer.WriteUInt32(AbilityValues, true);
        writer.WriteF32(FlySpeed, true);
        writer.WriteF32(VerticalFlySpeed, true);
        writer.WriteF32(WalkSpeed, true);
    }
}
