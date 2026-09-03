using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class TintMapColor : DataType {
    public int[] Colors = new int[4];

    public override void Write(ref BinaryWriter writer) {
        if (Colors.Length != 4) throw new InvalidOperationException("Tint maps must contain four colors.");
        foreach (int color in Colors) writer.WriteInt32(color, true);
    }

    public override void Read(ref BinaryReader reader) {
        Colors = new int[4];
        for (int index = 0; index < Colors.Length; index++) Colors[index] = reader.ReadInt32(true);
    }
}
