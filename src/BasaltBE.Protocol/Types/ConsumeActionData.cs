using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ConsumeActionData : ItemStackRequestActionData {
    public override ItemStackRequestActionType ActionType => ItemStackRequestActionType.Consume;
    public byte Amount;
    public ItemStackSlotInfoData Source = new();
    public override void Read(ref BinaryReader reader) { Amount = reader.ReadUInt8(); Source.Read(ref reader); }
    public override void Write(ref BinaryWriter writer) { writer.WriteUInt8(Amount); Source.Write(ref writer); }
}
