using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class DimensionDefinition : DataType {
    public int MinimumY;
    public int HeightRange;
    public GeneratorType GeneratorType;
    public int RuntimeId;
    public Uuid PackId = new();
    public string DefaultBiome = string.Empty;
    public int CloudHeight;
    public bool RenderClouds;

    public override void Read(ref BinaryReader reader) {
        MinimumY = reader.ReadZigZag();
        HeightRange = reader.ReadZigZag();
        GeneratorType = (GeneratorType)reader.ReadZigZag();
        RuntimeId = reader.ReadZigZag();
        PackId.Read(ref reader);
        DefaultBiome = reader.ReadVarString();
        CloudHeight = reader.ReadZigZag();
        RenderClouds = reader.ReadBool();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZag(MinimumY);
        writer.WriteZigZag(HeightRange);
        writer.WriteZigZag((int)GeneratorType);
        writer.WriteZigZag(RuntimeId);
        PackId.Write(ref writer);
        writer.WriteVarString(DefaultBiome);
        writer.WriteZigZag(CloudHeight);
        writer.WriteBool(RenderClouds);
    }
}
