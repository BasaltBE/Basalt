#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackRequestBeaconPaymentAction : ItemStackRequestActionVariant {
    public ItemStackRequestActionType ActionType = global::BedrockProtocol.Enums.ItemStackRequestActionType.ScreenBeaconPayment;
    public int PrimaryEffectId;
    public int SecondaryEffectId;

    public void Read(BinaryReader reader) {
        PrimaryEffectId = reader.ReadZigZag();
        SecondaryEffectId = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(PrimaryEffectId);
        writer.WriteZigZag(SecondaryEffectId);
    }
}
