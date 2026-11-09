using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class DropActionData : ItemStackRequestActionData {
    public override ItemStackRequestActionType ActionType => ItemStackRequestActionType.Drop;
    public byte Amount;
    public ItemStackSlotInfoData Source = new();
    public bool Randomly;
    public override void Read(ref BinaryReader reader) { Amount = reader.ReadUInt8(); Source.Read(ref reader); Randomly = reader.ReadBool(); }
    public override void Write(ref BinaryWriter writer) { writer.WriteUInt8(Amount); Source.Write(ref writer); writer.WriteBool(Randomly); }
}
