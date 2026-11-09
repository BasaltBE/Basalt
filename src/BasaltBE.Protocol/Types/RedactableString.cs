using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class RedactableString : DataType {
    public string Unredacted = string.Empty;
    public string? Redacted;

    public override void Read(ref BinaryReader reader) {
        Unredacted = reader.ReadVarString();
        Redacted = reader.ReadBool() ? reader.ReadVarString() : null;
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Unredacted);
        writer.WriteBool(Redacted is not null);
        if (Redacted is string redacted) writer.WriteVarString(redacted);
    }
}
