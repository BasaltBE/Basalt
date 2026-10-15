using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class PlayerListEntry : DataType {
    public PlayerListAction Action;
    public PlayerListAddEntry? AddEntry;
    public PlayerListRemoveEntry? RemoveEntry;

    public override void Read(ref BinaryReader reader) {
        Action = (PlayerListAction)reader.ReadUInt8();
        switch (Action) {
            case PlayerListAction.Add:
                AddEntry = new PlayerListAddEntry();
                AddEntry.Read(ref reader);
                RemoveEntry = null;
                break;
            case PlayerListAction.Remove:
                RemoveEntry = new PlayerListRemoveEntry();
                RemoveEntry.Read(ref reader);
                AddEntry = null;
                break;
            default:
                throw new InvalidDataException("Unknown player list action.");
        }
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt8((byte)Action);
        switch (Action) {
            case PlayerListAction.Add:
                (AddEntry ?? throw new InvalidDataException("Player list add entry is missing.")).Write(ref writer);
                break;
            case PlayerListAction.Remove:
                (RemoveEntry ?? throw new InvalidDataException("Player list remove entry is missing.")).Write(ref writer);
                break;
            default:
                throw new InvalidDataException("Unknown player list action.");
        }
    }
}
