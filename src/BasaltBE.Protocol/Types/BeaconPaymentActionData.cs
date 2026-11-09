using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class BeaconPaymentActionData : ItemStackRequestActionData {
    public override ItemStackRequestActionType ActionType => ItemStackRequestActionType.ScreenBeaconPayment;
    public int PrimaryEffectId;
    public int SecondaryEffectId;
    public override void Read(ref BinaryReader reader) { PrimaryEffectId = reader.ReadVarInt(); SecondaryEffectId = reader.ReadVarInt(); }
    public override void Write(ref BinaryWriter writer) { writer.WriteVarInt(PrimaryEffectId); writer.WriteVarInt(SecondaryEffectId); }
}
