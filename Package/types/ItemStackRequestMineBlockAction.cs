#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackRequestMineBlockAction : ItemStackRequestActionVariant {
    public ItemStackRequestActionType ActionType = global::BedrockProtocol.Enums.ItemStackRequestActionType.ScreenHUDMineBlock;
    public int Slot;
    public int PredictedDurability;
    public int NetIdVariant;

    public void Read(BinaryReader reader) {
        Slot = reader.ReadZigZag();
        PredictedDurability = reader.ReadZigZag();
        NetIdVariant = reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(Slot);
        writer.WriteZigZag(PredictedDurability);
        writer.WriteInt32(NetIdVariant, true);
    }
}
