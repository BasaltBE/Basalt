using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class PlayerListRemoveEntry : DataType {
    public Uuid Uuid = new();

    public override void Read(ref BinaryReader reader) => Uuid.Read(ref reader);

    public override void Write(ref BinaryWriter writer) => Uuid.Write(ref writer);
}
