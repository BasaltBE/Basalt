using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class MultiRecipeData : DataType
{
    public byte[] Uuid = new byte[16];
    public uint RecipeNetworkId;

    public void Read(BinaryReader reader)
    {
        reader.ReadBytes(16).CopyTo(Uuid.AsSpan());
        RecipeNetworkId = reader.ReadVarUInt();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteBytes(Uuid);
        writer.WriteVarUInt(RecipeNetworkId);
    }
}
