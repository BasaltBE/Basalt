using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class PersonaPiece : DataType {
    /// <summary>
    /// Unique id of the persona piece.
    /// </summary>
    public string PieceId = string.Empty;

    /// <summary>
    /// Type name of the persona piece.
    /// </summary>
    public string PieceType = string.Empty;

    /// <summary>
    /// Pack id of the persona piece.
    /// </summary>
    public string PackId = string.Empty;

    /// <summary>
    /// Whether this piece is default.
    /// </summary>
    public bool Default;

    /// <summary>
    /// Product id of the persona piece.
    /// </summary>
    public string ProductId = string.Empty;

    public void Read(BinaryReader reader) {
        PieceId = reader.ReadVarString();
        PieceType = reader.ReadVarString();
        PackId = reader.ReadVarString();
        Default = reader.ReadBool();
        ProductId = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(PieceId);
        writer.WriteVarString(PieceType);
        writer.WriteVarString(PackId);
        writer.WriteBool(Default);
        writer.WriteVarString(ProductId);
    }
}
