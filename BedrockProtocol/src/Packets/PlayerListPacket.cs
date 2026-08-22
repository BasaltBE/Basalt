using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(63)]
public sealed class PlayerListPacket : DataPacket {
    public PlayerListPacketType Action;
    public PlayerListAddEntry[] AddEntries = [];
    public Uuid[] RemoveEntries = [];

    public override void Serialize(ref BinaryWriter writer) {
        int count = Action == PlayerListPacketType.Add ? AddEntries.Length : RemoveEntries.Length;
        writer.WriteVarUInt((uint)count);
        if (Action == PlayerListPacketType.Add) {
            for (int i = 0; i < AddEntries.Length; i++) {
                writer.WriteVarUInt(1);
                writer.WriteUInt8((byte)PlayerListPacketType.Add);
                AddEntries[i].Write(ref writer);
            }
        }
        else {
            for (int i = 0; i < RemoveEntries.Length; i++) {
                writer.WriteVarUInt(0);
                writer.WriteUInt8((byte)PlayerListPacketType.Remove);
                RemoveEntries[i].Write(ref writer);
            }
        }
    }

    public override void Deserialize(ref BinaryReader reader) {
        int count = checked((int)reader.ReadVarUInt());
        PlayerListAddEntry[] addEntries = new PlayerListAddEntry[count];
        Uuid[] removeEntries = new Uuid[count];
        int addCount = 0;
        int removeCount = 0;
        for (int i = 0; i < count; i++) {
            bool add = reader.ReadVarUInt() == 1;
            PlayerListPacketType action = (PlayerListPacketType)reader.ReadUInt8();
            if (add && action == PlayerListPacketType.Add) {
                addEntries[addCount] = new PlayerListAddEntry();
                addEntries[addCount++].Read(ref reader);
                continue;
            }

            if (!add && action == PlayerListPacketType.Remove) {
                removeEntries[removeCount] = new Uuid();
                removeEntries[removeCount++].Read(ref reader);
                continue;
            }

            throw new FormatException("Unsupported player list entry type.");
        }

        AddEntries = new PlayerListAddEntry[addCount];
        Array.Copy(addEntries, AddEntries, addCount);
        RemoveEntries = new Uuid[removeCount];
        Array.Copy(removeEntries, RemoveEntries, removeCount);
    }
}
