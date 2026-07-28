using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class TrustedSkinFlag : DataType {
    /// <summary>Whether the client marked the skin as trusted.</summary>
    public bool Trusted;

    public void Read(BinaryReader reader) => Trusted = reader.ReadBool();

    public void Write(BinaryWriter writer) => writer.WriteBool(Trusted);
}
