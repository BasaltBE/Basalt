using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SerializedPersonaPieceHandle {
    public string PieceId = string.Empty;
    public PieceType PieceType;
    public UUID PackId = new();
    public bool IsDefaultPiece;
    public string ProductId = string.Empty;

    public void Read(BinaryReader reader) {
        PieceId = reader.ReadVarString();
        PieceType = (global::BedrockProtocol.Enums.PieceType)reader.ReadUInt32(true);
        PackId.Read(reader);
        IsDefaultPiece = reader.ReadBool();
        ProductId = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(PieceId);
        writer.WriteUInt32((uint)PieceType, true);
        PackId.Write(writer);
        writer.WriteBool(IsDefaultPiece);
        writer.WriteVarString(ProductId);
    }
}
