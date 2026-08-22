using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class SpawnSettings : DataType {
    public SpawnBiomeType SpawnBiomeType;
    public string UserDefinedBiomeName = string.Empty;
    public int Dimension;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteInt16((short)SpawnBiomeType, true);
        writer.WriteVarString(UserDefinedBiomeName);
        writer.WriteZigZag(Dimension);
    }

    public override void Read(ref BinaryReader reader) {
        SpawnBiomeType = (SpawnBiomeType)reader.ReadInt16(true);
        UserDefinedBiomeName = reader.ReadVarString();
        Dimension = reader.ReadZigZag();
    }
}
