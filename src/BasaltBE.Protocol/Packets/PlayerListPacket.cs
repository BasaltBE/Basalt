using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(63)]
public sealed class PlayerListPacket : DataPacket
{
    public PlayerListEntry[] Entries = [];

    public override void Deserialize(ref BinaryReader reader)
    {
        uint count = reader.ReadVarUInt();
        if (count > 1000)
            throw new InvalidDataException("A player list cannot contain more than 1000 entries.");
        Entries = new PlayerListEntry[(int)count];
        for (int i = 0; i < Entries.Length; i++)
        {
            Entries[i] = new PlayerListEntry();
            Entries[i].Read(ref reader);
        }
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        if (Entries.Length > 1000)
            throw new InvalidDataException("A player list cannot contain more than 1000 entries.");
        writer.WriteVarUInt((uint)Entries.Length);
        foreach (PlayerListEntry entry in Entries)
            entry.Write(ref writer);
    }
}
