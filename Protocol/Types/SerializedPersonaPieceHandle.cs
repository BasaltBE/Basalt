using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class SerializedPersonaPieceHandle : DataType {
    /// <summary>The persona piece identifier.</summary>
    public string PieceId = string.Empty;

    /// <summary>The persona piece type.</summary>
    public string PieceType = string.Empty;

    /// <summary>The resource pack identifier.</summary>
    public string PackId = string.Empty;

    /// <summary>Whether the persona piece is enabled by default.</summary>
    public bool Default;

    /// <summary>The product identifier.</summary>
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
