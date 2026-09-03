using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ItemStackRequestData : DataType {
    public int ClientRequestId;
    public ItemStackRequestAction[] Actions = [];
    public string[] StringsToFilter = [];
    public TextProcessingEventOrigin StringsToFilterOrigin;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarInt(ClientRequestId);
        writer.WriteVarUInt((uint)Actions.Length);
        foreach (ItemStackRequestAction action in Actions) action.Write(ref writer);
        writer.WriteVarUInt((uint)StringsToFilter.Length);
        foreach (string value in StringsToFilter) writer.WriteVarString(value);
        writer.WriteInt32((int)StringsToFilterOrigin, true);
    }

    public override void Read(ref BinaryReader reader) {
        ClientRequestId = reader.ReadVarInt();
        Actions = new ItemStackRequestAction[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < Actions.Length; index++) {
            ItemStackRequestAction action = new();
            action.Read(ref reader);
            Actions[index] = action;
        }
        StringsToFilter = new string[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < StringsToFilter.Length; index++) StringsToFilter[index] = reader.ReadVarString();
        StringsToFilterOrigin = (TextProcessingEventOrigin)reader.ReadInt32(true);
    }
}
