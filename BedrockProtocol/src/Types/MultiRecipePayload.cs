using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class MultiRecipePayload : DataType {
    public Uuid Uuid = new();
    public uint NetId;

    public override void Write(ref BinaryWriter writer) {
        Uuid.Write(ref writer);
        writer.WriteVarUInt(NetId);
    }

    public override void Read(ref BinaryReader reader) {
        Uuid.Read(ref reader);
        NetId = reader.ReadVarUInt();
    }
}
