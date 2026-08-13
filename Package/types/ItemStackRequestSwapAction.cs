#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackRequestSwapAction : ItemStackRequestActionVariant {
    public ItemStackRequestActionType ActionType = global::BedrockProtocol.Enums.ItemStackRequestActionType.Swap;
    public ItemStackRequestSlotInfo Source = new();
    public ItemStackRequestSlotInfo Destination = new();

    public void Read(BinaryReader reader) {
        Source.Read(reader);
        Destination.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        Source.Write(writer);
        Destination.Write(writer);
    }
}
