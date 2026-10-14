using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class MineBlockActionData : ItemStackRequestActionData {
    public override ItemStackRequestActionType ActionType => ItemStackRequestActionType.ScreenHudMineBlock;
    public int Slot;
    public int PredictedDurability;
    public int NetIdVariant;
    public override void Read(ref BinaryReader reader) { Slot = reader.ReadVarInt(); PredictedDurability = reader.ReadVarInt(); NetIdVariant = reader.ReadInt32(true); }
    public override void Write(ref BinaryWriter writer) { writer.WriteVarInt(Slot); writer.WriteVarInt(PredictedDurability); writer.WriteInt32(NetIdVariant, true); }
}
