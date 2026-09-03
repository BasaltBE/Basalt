using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class SerializedPersonaPieceHandle : DataType {
    public string PieceId = string.Empty;
    public PersonaPieceType PieceType;
    public Uuid PackId = new();
    public bool DefaultPiece;
    public string ProductId = string.Empty;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(PieceId);
        writer.WriteUInt32((uint)PieceType, true);
        PackId.Write(ref writer);
        writer.WriteBool(DefaultPiece);
        writer.WriteVarString(ProductId);
    }

    public override void Read(ref BinaryReader reader) {
        PieceId = reader.ReadVarString();
        PieceType = (PersonaPieceType)reader.ReadUInt32(true);
        PackId.Read(ref reader);
        DefaultPiece = reader.ReadBool();
        ProductId = reader.ReadVarString();
    }
}
