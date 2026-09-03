using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class SerializableVoxelShape : DataType {
    public SerializableCells Cells = new();
    public float[] XCoordinates = [];
    public float[] YCoordinates = [];
    public float[] ZCoordinates = [];

    public override void Write(ref BinaryWriter writer) {
        Cells.Write(ref writer);
        WriteCoordinates(ref writer, XCoordinates);
        WriteCoordinates(ref writer, YCoordinates);
        WriteCoordinates(ref writer, ZCoordinates);
    }

    public override void Read(ref BinaryReader reader) {
        Cells.Read(ref reader);
        XCoordinates = ReadCoordinates(ref reader);
        YCoordinates = ReadCoordinates(ref reader);
        ZCoordinates = ReadCoordinates(ref reader);
    }

    private static void WriteCoordinates(ref BinaryWriter writer, float[] coordinates) {
        writer.WriteVarUInt((uint)coordinates.Length);
        foreach (float coordinate in coordinates)
            writer.WriteF32(coordinate, true);
    }

    private static float[] ReadCoordinates(ref BinaryReader reader) {
        int count = checked((int)reader.ReadVarUInt());
        float[] coordinates = new float[count];
        for (int index = 0; index < count; index++)
            coordinates[index] = reader.ReadF32(true);
        return coordinates;
    }
}
