using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(122)]
public sealed class BiomeDefinitionListPacket : DataPacket {
    public Dictionary<ushort, BiomeDefinitionData> Definitions = [];
    public string[] Strings = [];

    public override void Deserialize(ref BinaryReader reader) {
        uint definitionCount = reader.ReadVarUInt();
        if (definitionCount > ushort.MaxValue || definitionCount > (uint)(reader.Remaining / sizeof(ushort)))
            throw new InvalidDataException("Biome definition count exceeds the remaining packet data.");

        Definitions = new Dictionary<ushort, BiomeDefinitionData>((int)definitionCount);
        for (int i = 0; i < definitionCount; i++) {
            ushort index = reader.ReadUInt16(true);
            BiomeDefinitionData definition = new();
            definition.Read(ref reader);
            Definitions.Add(index, definition);
        }

        uint stringCount = reader.ReadVarUInt();
        if (stringCount > ushort.MaxValue)
            throw new InvalidDataException("Biome string count exceeds the supported limit.");

        Strings = new string[checked((int)stringCount)];
        for (int i = 0; i < Strings.Length; i++)
            Strings[i] = reader.ReadVarString();
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Definitions.Count);
        foreach ((ushort index, BiomeDefinitionData definition) in Definitions) {
            writer.WriteUInt16(index, true);
            definition.Write(ref writer);
        }

        writer.WriteVarUInt((uint)Strings.Length);
        foreach (string biome in Strings)
            writer.WriteVarString(biome);
    }
}
