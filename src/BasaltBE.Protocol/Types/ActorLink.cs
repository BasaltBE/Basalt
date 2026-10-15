using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ActorLink : DataType {
    public long TargetA;
    public long TargetB;
    public ActorLinkType Type;
    public bool Immediate;
    public bool PassengerInitiated;
    public float VehicleAngularVelocity;

    public override void Read(ref BinaryReader reader) {
        TargetA = reader.ReadZigZong();
        TargetB = reader.ReadZigZong();
        Type = (ActorLinkType)reader.ReadUInt8();
        Immediate = reader.ReadBool();
        PassengerInitiated = reader.ReadBool();
        VehicleAngularVelocity = reader.ReadF32(true);
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZong(TargetA);
        writer.WriteZigZong(TargetB);
        writer.WriteUInt8((byte)Type);
        writer.WriteBool(Immediate);
        writer.WriteBool(PassengerInitiated);
        writer.WriteF32(VehicleAngularVelocity, true);
    }
}
