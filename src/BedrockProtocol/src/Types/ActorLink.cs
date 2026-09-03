using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ActorLink : DataType {
    public long TargetA;
    public long TargetB;
    public byte Type;
    public bool Immediate;
    public bool PassengerInitiated;
    public float VehicleAngularVelocity;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarLong(TargetA);
        writer.WriteVarLong(TargetB);
        writer.WriteUInt8(Type);
        writer.WriteBool(Immediate);
        writer.WriteBool(PassengerInitiated);
        writer.WriteF32(VehicleAngularVelocity, true);
    }

    public override void Read(ref BinaryReader reader) {
        TargetA = reader.ReadVarLong();
        TargetB = reader.ReadVarLong();
        Type = reader.ReadUInt8();
        Immediate = reader.ReadBool();
        PassengerInitiated = reader.ReadBool();
        VehicleAngularVelocity = reader.ReadF32(true);
    }
}
