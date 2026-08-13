#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackRequestCraftLoomAction : ItemStackRequestActionVariant {
    public ItemStackRequestActionType ActionType = global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftLoom;
    public string PatternNameId = string.Empty;
    public byte NumCrafts;

    public void Read(BinaryReader reader) {
        PatternNameId = reader.ReadVarString();
        NumCrafts = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(PatternNameId);
        writer.WriteUInt8(NumCrafts);
    }
}
