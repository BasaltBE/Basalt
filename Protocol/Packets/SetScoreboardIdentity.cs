using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.SetScoreboardIdentity)]
public sealed record SetScoreboardIdentityPacket : DataPacket
{
    public ScoreboardIdentityAction Action;
    public List<ScoreboardIdentityEntry> Entries = [];

    public override void Deserialize(Binary.BinaryReader reader)
    {
        Action = (ScoreboardIdentityAction)reader.ReadUInt8();
        int count = checked((int)reader.ReadVarUInt());
        Entries = new List<ScoreboardIdentityEntry>(count);

        for (int i = 0; i < count; i++)
        {
            long scoreboardId = reader.ReadZigZong();
            long entityUniqueId = 0;

            if (Action == ScoreboardIdentityAction.Register)
            {
                entityUniqueId = reader.ReadZigZong();
            }

            Entries.Add(new ScoreboardIdentityEntry
            {
                ScoreboardId = scoreboardId,
                EntityUniqueId = entityUniqueId
            });
        }
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteUInt8((byte)Action);
        writer.WriteVarUInt((uint)Entries.Count);

        for (int i = 0; i < Entries.Count; i++)
        {
            ScoreboardIdentityEntry entry = Entries[i];
            writer.WriteZigZong(entry.ScoreboardId);

            if (Action == ScoreboardIdentityAction.Register)
            {
                writer.WriteZigZong(entry.EntityUniqueId);
            }
        }
    }
}

public struct ScoreboardIdentityEntry
{
    public long ScoreboardId;
    public long EntityUniqueId;
}
