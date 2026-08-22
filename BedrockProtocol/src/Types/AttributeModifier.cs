using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class AttributeModifier : DataType {
    public string Id = string.Empty;
    public string Name = string.Empty;
    public float Amount;
    public int Operation;
    public int Operand;
    public bool Serializable;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Id);
        writer.WriteVarString(Name);
        writer.WriteF32(Amount, true);
        writer.WriteInt32(Operation, true);
        writer.WriteInt32(Operand, true);
        writer.WriteBool(Serializable);
    }

    public override void Read(ref BinaryReader reader) {
        Id = reader.ReadVarString();
        Name = reader.ReadVarString();
        Amount = reader.ReadF32(true);
        Operation = reader.ReadInt32(true);
        Operand = reader.ReadInt32(true);
        Serializable = reader.ReadBool();
    }
}
