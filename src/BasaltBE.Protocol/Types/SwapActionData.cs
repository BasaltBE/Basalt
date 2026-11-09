using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class SwapActionData : ItemStackRequestActionData {
    public override ItemStackRequestActionType ActionType => ItemStackRequestActionType.Swap;
    public ItemStackSlotInfoData Source = new();
    public ItemStackSlotInfoData Destination = new();
    public override void Read(ref BinaryReader reader) { Source.Read(ref reader); Destination.Read(ref reader); }
    public override void Write(ref BinaryWriter writer) { Source.Write(ref writer); Destination.Write(ref writer); }
}
