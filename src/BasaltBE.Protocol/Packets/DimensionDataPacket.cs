using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(180)]
public sealed class DimensionDataPacket : DataPacket {
    public Dictionary<string, DimensionDefinition> Definitions = new(StringComparer.Ordinal);

    public override void Deserialize(ref BinaryReader reader) {
        uint definitionCount = reader.ReadVarUInt();
        if (definitionCount > int.MaxValue || definitionCount > (uint)(reader.Remaining / 2))
            throw new InvalidDataException("Dimension definition count exceeds the remaining packet data.");
        Definitions = new Dictionary<string, DimensionDefinition>((int)definitionCount, StringComparer.Ordinal);
        for (int index = 0; index < definitionCount; index++) {
            string identifier = reader.ReadVarString();
            DimensionDefinition definition = new();
            definition.Read(ref reader);
            Definitions.Add(identifier, definition);
        }
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Definitions.Count);
        foreach ((string identifier, DimensionDefinition definition) in Definitions) {
            writer.WriteVarString(identifier);
            definition.Write(ref writer);
        }
    }
}
