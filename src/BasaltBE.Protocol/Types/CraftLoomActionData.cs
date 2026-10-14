using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class CraftLoomActionData : ItemStackRequestActionData {
    public override ItemStackRequestActionType ActionType => ItemStackRequestActionType.CraftLoom;
    public string PatternNameId = string.Empty;
    public byte NumberOfRequestedCrafts;
    public override void Read(ref BinaryReader reader) { PatternNameId = reader.ReadVarString(); NumberOfRequestedCrafts = reader.ReadUInt8(); }
    public override void Write(ref BinaryWriter writer) { writer.WriteVarString(PatternNameId); writer.WriteUInt8(NumberOfRequestedCrafts); }
}
