using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class CraftRepairAndDisenchantActionData : ItemStackRequestActionData {
    public override ItemStackRequestActionType ActionType => ItemStackRequestActionType.CraftRepairAndDisenchant;
    public int RecipeNetId;
    public byte NumberOfRequestedCrafts;
    public int RepairCost;
    public override void Read(ref BinaryReader reader) { RecipeNetId = reader.ReadInt32(true); NumberOfRequestedCrafts = reader.ReadUInt8(); RepairCost = reader.ReadVarInt(); }
    public override void Write(ref BinaryWriter writer) { writer.WriteInt32(RecipeNetId, true); writer.WriteUInt8(NumberOfRequestedCrafts); writer.WriteVarInt(RepairCost); }
}
