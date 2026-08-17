#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackRequestCraftNonImplementedDeprecatedAction : ItemStackRequestActionVariant {
    public ItemStackRequestActionType ActionType = global::BedrockProtocol.Enums.ItemStackRequestActionType.CraftNonImplemented;

    #pragma warning disable CA1822

    public void Read(BinaryReader reader) {
    }

    public void Write(BinaryWriter writer) {
    }

    #pragma warning restore CA1822
}
