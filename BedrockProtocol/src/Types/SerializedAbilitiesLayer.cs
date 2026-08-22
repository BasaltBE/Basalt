using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class SerializedAbilitiesLayer : DataType {
    public ushort Layer;
    public uint AbilitiesSet;
    public uint AbilityValues;
    public float FlySpeed;
    public float VerticalFlySpeed;
    public float WalkSpeed;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt16(Layer, true);
        writer.WriteUInt32(AbilitiesSet, true);
        writer.WriteUInt32(AbilityValues, true);
        writer.WriteF32(FlySpeed, true);
        writer.WriteF32(VerticalFlySpeed, true);
        writer.WriteF32(WalkSpeed, true);
    }

    public override void Read(ref BinaryReader reader) {
        Layer = reader.ReadUInt16(true);
        AbilitiesSet = reader.ReadUInt32(true);
        AbilityValues = reader.ReadUInt32(true);
        FlySpeed = reader.ReadF32(true);
        VerticalFlySpeed = reader.ReadF32(true);
        WalkSpeed = reader.ReadF32(true);
    }
}
