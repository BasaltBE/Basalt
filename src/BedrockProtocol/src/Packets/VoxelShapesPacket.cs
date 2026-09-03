using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(337)]
public sealed class VoxelShapesPacket : DataPacket {
    public SerializableVoxelShape[] Shapes = [];
    public Dictionary<string, RegistryHandle> NameMap = new(StringComparer.Ordinal);
    public ushort CustomShapeCount;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Shapes.Length);
        foreach (SerializableVoxelShape shape in Shapes)
            shape.Write(ref writer);

        writer.WriteVarUInt((uint)NameMap.Count);
        foreach ((string name, RegistryHandle handle) in NameMap) {
            writer.WriteVarString(name);
            handle.Write(ref writer);
        }

        writer.WriteUInt16(CustomShapeCount, true);
    }

    public override void Deserialize(ref BinaryReader reader) {
        int shapeCount = checked((int)reader.ReadVarUInt());
        Shapes = new SerializableVoxelShape[shapeCount];
        for (int index = 0; index < shapeCount; index++) {
            SerializableVoxelShape shape = new();
            shape.Read(ref reader);
            Shapes[index] = shape;
        }

        int nameMapCount = checked((int)reader.ReadVarUInt());
        NameMap = new Dictionary<string, RegistryHandle>(nameMapCount, StringComparer.Ordinal);
        for (int index = 0; index < nameMapCount; index++) {
            string name = reader.ReadVarString();
            RegistryHandle handle = new();
            handle.Read(ref reader);
            NameMap.Add(name, handle);
        }

        CustomShapeCount = reader.ReadUInt16(true);
    }
}
