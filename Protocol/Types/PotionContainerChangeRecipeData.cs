using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class PotionContainerChangeRecipeData : DataType
{
    public int InputItemId;
    public int ReagentItemId;
    public int OutputItemId;

    public void Read(BinaryReader reader)
    {
        InputItemId = reader.ReadZigZag();
        ReagentItemId = reader.ReadZigZag();
        OutputItemId = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteZigZag(InputItemId);
        writer.WriteZigZag(ReagentItemId);
        writer.WriteZigZag(OutputItemId);
    }
}
