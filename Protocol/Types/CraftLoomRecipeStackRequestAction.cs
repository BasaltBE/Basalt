using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CraftLoomRecipeStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 17;
    public string Pattern { get; set; } = string.Empty;
    public byte TimesCrafted { get; set; }

    public void Read(BinaryReader reader)
    {
        Pattern = reader.ReadVarString();
        TimesCrafted = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarString(Pattern);
        writer.WriteUInt8(TimesCrafted);
    }
}
