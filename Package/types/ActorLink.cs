using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ActorLink {
    public ActorUniqueID TargetA = new();
    public ActorUniqueID TargetB = new();
    public ActorLinkType Type;
    public bool Immediate;
    public bool PassengerInitiated;
    public float VehicleAngularVelocity;

    public void Read(BinaryReader reader) {
        TargetA.Read(reader);
        TargetB.Read(reader);
        Type = (global::BedrockProtocol.Enums.ActorLinkType)reader.ReadUInt8();
        Immediate = reader.ReadBool();
        PassengerInitiated = reader.ReadBool();
        VehicleAngularVelocity = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        TargetA.Write(writer);
        TargetB.Write(writer);
        writer.WriteUInt8((byte)Type);
        writer.WriteBool(Immediate);
        writer.WriteBool(PassengerInitiated);
        writer.WriteF32(VehicleAngularVelocity, true);
    }
}
