using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class CraftCreativeActionData : ItemStackRequestActionData {
    public override ItemStackRequestActionType ActionType => ItemStackRequestActionType.CraftCreative;
    public uint CreativeItemNetId;
    public byte NumberOfRequestedCrafts;
    public override void Read(ref BinaryReader reader) { CreativeItemNetId = reader.ReadVarUInt(); NumberOfRequestedCrafts = reader.ReadUInt8(); }
    public override void Write(ref BinaryWriter writer) { writer.WriteVarUInt(CreativeItemNetId); writer.WriteUInt8(NumberOfRequestedCrafts); }
}
