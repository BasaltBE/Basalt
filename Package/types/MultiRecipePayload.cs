using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class MultiRecipePayload {
    public UUID MultiRecipeUUID = new();
    public RecipeNetId NetId = new();

    public void Read(BinaryReader reader) {
        MultiRecipeUUID.Read(reader);
        NetId.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        MultiRecipeUUID.Write(writer);
        NetId.Write(writer);
    }
}
