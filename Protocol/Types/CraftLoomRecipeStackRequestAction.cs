using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CraftLoomRecipeStackRequestAction : IStackRequestAction, DataType
{
    public byte ActionType => 17;
    /// <summary>
    /// Loom pattern identifier string.
    /// </summary>
    public string Pattern = string.Empty;
    /// <summary>
    /// Times the loom recipe was crafted.
    /// </summary>
    public byte TimesCrafted;
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
