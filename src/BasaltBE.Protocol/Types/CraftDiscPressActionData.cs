using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class CraftDiscPressActionData : ItemStackRequestActionData {
    public override ItemStackRequestActionType ActionType => ItemStackRequestActionType.CraftReservedAction;
    public string ReservedId = string.Empty;
    public byte NumberOfCrafts;
    public override void Read(ref BinaryReader reader) { ReservedId = reader.ReadVarString(); NumberOfCrafts = reader.ReadUInt8(); }
    public override void Write(ref BinaryWriter writer) { writer.WriteVarString(ReservedId); writer.WriteUInt8(NumberOfCrafts); }
}
