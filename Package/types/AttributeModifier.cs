#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AttributeModifier {
    public string Id = string.Empty;
    public string Name = string.Empty;
    public float Amount;
    public int Operation;
    public int Operand;
    public bool IsSerializable;

    public void Read(BinaryReader reader) {
        Id = reader.ReadVarString();
        Name = reader.ReadVarString();
        Amount = reader.ReadF32(true);
        Operation = reader.ReadInt32(true);
        Operand = reader.ReadInt32(true);
        IsSerializable = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Id);
        writer.WriteVarString(Name);
        writer.WriteF32(Amount, true);
        writer.WriteInt32(Operation, true);
        writer.WriteInt32(Operand, true);
        writer.WriteBool(IsSerializable);
    }
}
