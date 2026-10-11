using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class SerializableVoxelShape : DataType {
    public SerializableVoxelCells Cells = new();
    public float[] XCoordinates = [];
    public float[] YCoordinates = [];
    public float[] ZCoordinates = [];

    public override void Read(ref BinaryReader reader) {
        Cells.Read(ref reader);
        XCoordinates = ReadCoordinates(ref reader);
        YCoordinates = ReadCoordinates(ref reader);
        ZCoordinates = ReadCoordinates(ref reader);
    }

    public override void Write(ref BinaryWriter writer) {
        Cells.Write(ref writer);
        WriteCoordinates(ref writer, XCoordinates);
        WriteCoordinates(ref writer, YCoordinates);
        WriteCoordinates(ref writer, ZCoordinates);
    }

    private static float[] ReadCoordinates(ref BinaryReader reader) {
        uint count = reader.ReadVarUInt();
        if (count is < 1 or > 128)
            throw new InvalidDataException("Voxel shape coordinate count is invalid.");

        float[] coordinates = new float[(int)count];
        for (int index = 0; index < coordinates.Length; index++)
            coordinates[index] = reader.ReadF32(true);
        return coordinates;
    }

    private static void WriteCoordinates(ref BinaryWriter writer, float[] coordinates) {
        if (coordinates.Length is < 1 or > 128)
            throw new InvalidDataException("Voxel shape coordinate count is invalid.");

        writer.WriteVarUInt((uint)coordinates.Length);
        foreach (float coordinate in coordinates)
            writer.WriteF32(coordinate, true);
    }
}
