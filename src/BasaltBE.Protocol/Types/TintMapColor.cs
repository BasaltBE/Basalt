using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class TintMapColor : DataType {
    public int[] Colors = new int[4];

    public override void Read(ref BinaryReader reader) {
        for (int index = 0; index < Colors.Length; index++)
            Colors[index] = reader.ReadInt32(true);
    }

    public override void Write(ref BinaryWriter writer) {
        foreach (int color in Colors)
            writer.WriteInt32(color, true);
    }
}
