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
        global::BedrockProtocol.Enums.ItemStackRequestActionType constValue0 = (global::BedrockProtocol.Enums.ItemStackRequestActionType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.ItemStackRequestActionType.ScreenBeaconPayment) {
            throw new FormatException($"Expected screenbeaconpayment for ActionType, got {constValue0}.");
        }
        PrimaryEffectId = reader.ReadZigZag();
        SecondaryEffectId = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)(byte)global::BedrockProtocol.Enums.ItemStackRequestActionType.ScreenBeaconPayment);
        writer.WriteZigZag(PrimaryEffectId);
        writer.WriteZigZag(SecondaryEffectId);
    }
}
