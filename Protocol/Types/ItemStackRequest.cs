using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ItemStackRequest : DataType {
    /// <summary>
    /// Unique request id from the client.
    /// </summary>
    public int RequestId;
    /// <summary>
    /// Actions included in this request.
    /// </summary>
    public List<IStackRequestAction> Actions = [];
    /// <summary>
    /// Filter strings used by UI actions.
    /// </summary>
    public List<string> FilterStrings = [];
    /// <summary>
    /// Cause id for filtering.
    /// </summary>
    public int FilterCause;
    public void Read(BinaryReader reader) {
        RequestId = reader.ReadZigZag();

        int actionCount = checked((int)reader.ReadVarUInt());
        Actions = new(actionCount);
        for (int i = 0; i < actionCount; i++) {
            byte type = reader.ReadUInt8();
            IStackRequestAction action = StackRequestActions.Create(type);
            action.Read(reader);
            Actions.Add(action);
        }

        int filterCount = checked((int)reader.ReadVarUInt());
        FilterStrings = new(filterCount);
        for (int i = 0; i < filterCount; i++) {
            FilterStrings.Add(reader.ReadVarString());
        }

        FilterCause = reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(RequestId);
        writer.WriteVarUInt((uint)Actions.Count);
        for (int i = 0; i < Actions.Count; i++) {
            writer.WriteUInt8(Actions[i].ActionType);
            Actions[i].Write(writer);
        }

        writer.WriteVarUInt((uint)FilterStrings.Count);
        for (int i = 0; i < FilterStrings.Count; i++) {
            writer.WriteVarString(FilterStrings[i]);
        }

        writer.WriteInt32(FilterCause, true);
    }
}
