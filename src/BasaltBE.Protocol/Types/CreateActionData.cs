using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class CreateActionData : ItemStackRequestActionData {
    public override ItemStackRequestActionType ActionType => ItemStackRequestActionType.Create;
    public byte ResultsIndex;
    public override void Read(ref BinaryReader reader) => ResultsIndex = reader.ReadUInt8();
    public override void Write(ref BinaryWriter writer) => writer.WriteUInt8(ResultsIndex);
}
