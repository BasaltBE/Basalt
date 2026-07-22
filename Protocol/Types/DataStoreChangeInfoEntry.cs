using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Types;

public static class DataStoreChangeInfoEntry {
    public static DataStoreChangeInfo Read(Binary.BinaryReader reader) {
        DataStoreChangeAction action = (DataStoreChangeAction)reader.ReadVarInt();
        DataStoreChangeInfo entry = action switch {
            DataStoreChangeAction.Update => new DataStoreUpdate(),
            DataStoreChangeAction.Change => new DataStoreChange(),
            DataStoreChangeAction.Removal => new DataStoreRemoval(),
            _ => throw new NotSupportedException($"Unsupported data store change action {action}.")
        };

        entry.Read(reader);
        return entry;
    }

    public static void Write(Binary.BinaryWriter writer, DataStoreChangeInfo entry) {
        writer.WriteVarInt((int)entry.Action);
        entry.Write(writer);
    }
}
