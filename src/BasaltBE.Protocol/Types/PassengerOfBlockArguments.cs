using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class PassengerOfBlockArguments : DataType {
    public BlockPos Position = new();
    public Vec3 Offset = new();
    public float Rotation;
    public float RotationLimit;
    public PassengerOfBlockEmoteType EmoteType;

    public override void Read(ref BinaryReader reader) {
        Position.Read(ref reader);
        Offset.Read(ref reader);
        Rotation = reader.ReadF32(true);
        RotationLimit = reader.ReadF32(true);
        EmoteType = (PassengerOfBlockEmoteType)reader.ReadUInt8();
    }

    public override void Write(ref BinaryWriter writer) {
        Position.Write(ref writer);
        Offset.Write(ref writer);
        writer.WriteF32(Rotation, true);
        writer.WriteF32(RotationLimit, true);
        writer.WriteUInt8((byte)EmoteType);
    }
}
