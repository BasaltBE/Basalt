using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class SerializedPersonaPieceHandle : DataType {
    public string PieceId = string.Empty;
    public PersonaPieceType PieceType;
    public Uuid PackId = new();
    public bool IsDefaultPiece;
    public string ProductId = string.Empty;

    public override void Read(ref BinaryReader reader) {
        PieceId = reader.ReadVarString();
        PieceType = (PersonaPieceType)reader.ReadUInt32(true);
        PackId.Read(ref reader);
        IsDefaultPiece = reader.ReadBool();
        ProductId = reader.ReadVarString();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(PieceId);
        writer.WriteUInt32((uint)PieceType, true);
        PackId.Write(ref writer);
        writer.WriteBool(IsDefaultPiece);
        writer.WriteVarString(ProductId);
    }
}
