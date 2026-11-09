using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ItemStackRequestData : DataType {
    public int ClientRequestId;
    public ItemStackRequestActionData[] Actions = [];
    public string[] StringsToFilter = [];
    public TextProcessingEventOrigin StringsToFilterOrigin;

    public override void Read(ref BinaryReader reader) {
        ClientRequestId = reader.ReadVarInt();
        uint actionCount = reader.ReadVarUInt();
        if (actionCount is 0 or > 100) throw new InvalidDataException("Item stack request action count must be from 1 to 100.");
        Actions = new ItemStackRequestActionData[(int)actionCount];
        for (int i = 0; i < Actions.Length; i++) Actions[i] = ItemStackRequestActionData.ReadAction(ref reader);
        uint stringCount = reader.ReadVarUInt();
        StringsToFilter = new string[(int)stringCount];
        for (int i = 0; i < StringsToFilter.Length; i++) {
            StringsToFilter[i] = reader.ReadVarString();
            if (StringsToFilter[i].Length > 1000) throw new InvalidDataException("Item stack request filter strings cannot exceed 1000 characters.");
        }
        StringsToFilterOrigin = (TextProcessingEventOrigin)reader.ReadInt32(true);
    }

    public override void Write(ref BinaryWriter writer) {
        if (Actions.Length is 0 or > 100) throw new InvalidDataException("Item stack request action count must be from 1 to 100.");
        writer.WriteVarInt(ClientRequestId);
        writer.WriteVarUInt((uint)Actions.Length);
        foreach (ItemStackRequestActionData action in Actions) action.WriteAction(ref writer);
        writer.WriteVarUInt((uint)StringsToFilter.Length);
        foreach (string value in StringsToFilter) {
            if (value.Length > 1000) throw new InvalidDataException("Item stack request filter strings cannot exceed 1000 characters.");
            writer.WriteVarString(value);
        }
        writer.WriteInt32((int)StringsToFilterOrigin, true);
    }
}
