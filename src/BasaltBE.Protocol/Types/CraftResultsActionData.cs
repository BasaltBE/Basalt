using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class CraftResultsActionData : ItemStackRequestActionData {
    public override ItemStackRequestActionType ActionType => ItemStackRequestActionType.CraftResults;
    public NetworkItemInstanceDescriptorData[] CraftResults = [];
    public byte NumberOfCrafts;
    public override void Read(ref BinaryReader reader) {
        uint count = reader.ReadVarUInt();
        if (count == 0) throw new InvalidDataException("Craft results must contain at least one item.");
        CraftResults = new NetworkItemInstanceDescriptorData[(int)count];
        for (int i = 0; i < CraftResults.Length; i++) { CraftResults[i] = new NetworkItemInstanceDescriptorData(); CraftResults[i].Read(ref reader); }
        NumberOfCrafts = reader.ReadUInt8();
    }
    public override void Write(ref BinaryWriter writer) {
        if (CraftResults.Length == 0) throw new InvalidDataException("Craft results must contain at least one item.");
        writer.WriteVarUInt((uint)CraftResults.Length);
        foreach (NetworkItemInstanceDescriptorData item in CraftResults) item.Write(ref writer);
        writer.WriteUInt8(NumberOfCrafts);
    }
}
