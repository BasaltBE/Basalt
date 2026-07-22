using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.PlayerList)]
public sealed record PlayerListPacket : DataPacket {
    /// <summary>
    /// Action type for this player list update.
    /// </summary>
    public PlayerListActionType ActionType;

    /// <summary>
    /// Player list entries to add or remove.
    /// </summary>
    public List<PlayerListEntry> Entries = [];

    public override void Deserialize(Binary.BinaryReader reader) {
        ActionType = (PlayerListActionType)reader.ReadUInt8();
        int entryCount = checked((int)reader.ReadVarUInt());
        Entries = new List<PlayerListEntry>(entryCount);

        if (ActionType == PlayerListActionType.Add) {
            for (int i = 0; i < entryCount; i++) {
                PlayerListEntry entry = new();
                entry.Read(reader);
                Entries.Add(entry);
            }

            for (int i = 0; i < entryCount; i++) {
                Entries[i].Skin.Trusted = reader.ReadBool();
            }

            return;
        }

        if (ActionType == PlayerListActionType.Remove) {
            for (int i = 0; i < entryCount; i++) {
                PlayerListEntry entry = new() {
                    Uuid = UUID.Read(reader)
                };
                Entries.Add(entry);
            }

            return;
        }

        throw new InvalidOperationException($"Unknown player list action type {(byte)ActionType}.");
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteUInt8((byte)ActionType);
        writer.WriteVarUInt((uint)Entries.Count);

        if (ActionType == PlayerListActionType.Add) {
            for (int i = 0; i < Entries.Count; i++) {
                Entries[i].Write(writer);
            }

            for (int i = 0; i < Entries.Count; i++) {
                writer.WriteBool(Entries[i].Skin.Trusted);
            }

            return;
        }

        if (ActionType == PlayerListActionType.Remove) {
            for (int i = 0; i < Entries.Count; i++) {
                UUID.Write(writer, Entries[i].Uuid);
            }

            return;
        }

        throw new InvalidOperationException($"Unknown player list action type {(byte)ActionType}.");
    }
}
