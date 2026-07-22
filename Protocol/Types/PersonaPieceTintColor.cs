using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class PersonaPieceTintColor : DataType {
    /// <summary>
    /// Piece type this tint applies to.
    /// </summary>
    public string PieceType = string.Empty;

    /// <summary>
    /// Tint colors for this piece.
    /// </summary>
    public List<string> Colors = [];

    public void Read(BinaryReader reader) {
        PieceType = reader.ReadVarString();
        int count = checked((int)reader.ReadUInt32(true));
        Colors = new List<string>(count);
        for (int i = 0; i < count; i++) {
            Colors.Add(reader.ReadVarString());
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(PieceType);
        writer.WriteUInt32((uint)Colors.Count, true);
        for (int i = 0; i < Colors.Count; i++) {
            writer.WriteVarString(Colors[i]);
        }
    }
}
