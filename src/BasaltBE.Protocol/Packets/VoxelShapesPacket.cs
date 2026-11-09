using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(337)]
public sealed class VoxelShapesPacket : DataPacket {
    public SerializableVoxelShape[] Shapes = [];
    public Dictionary<string, VoxelShapeRegistryHandle> NameMap = [];
    public ushort CustomShapeCount;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Shapes.Length);
        foreach (SerializableVoxelShape shape in Shapes)
            shape.Write(ref writer);

        writer.WriteVarUInt((uint)NameMap.Count);
        foreach ((string name, VoxelShapeRegistryHandle handle) in NameMap) {
            writer.WriteVarString(name);
            handle.Write(ref writer);
        }

        writer.WriteUInt16(CustomShapeCount, true);
    }

    public override void Deserialize(ref BinaryReader reader) {
        uint shapeCount = reader.ReadVarUInt();
        Shapes = new SerializableVoxelShape[(int)shapeCount];
        for (int index = 0; index < Shapes.Length; index++) {
            SerializableVoxelShape shape = new();
            shape.Read(ref reader);
            Shapes[index] = shape;
        }

        uint nameCount = reader.ReadVarUInt();
        NameMap = new Dictionary<string, VoxelShapeRegistryHandle>((int)nameCount);
        for (int index = 0; index < nameCount; index++) {
            string name = reader.ReadVarString();
            VoxelShapeRegistryHandle handle = new();
            handle.Read(ref reader);
            NameMap.Add(name, handle);
        }

        CustomShapeCount = reader.ReadUInt16(true);
    }
}
