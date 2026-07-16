using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.SetScore)]
public sealed record SetScorePacket : DataPacket
{
  public ScoreboardActionType ActionType;
  public List<ScoreEntry> Entries = [];

  public override void Deserialize(Binary.BinaryReader reader)
  {
    ActionType = (ScoreboardActionType)reader.ReadUInt8();
    int count = checked((int)reader.ReadVarUInt());
    Entries = new List<ScoreEntry>(count);

    for (int i = 0; i < count; i++)
    {
      long scoreboardId = reader.ReadZigZong();
      string objectiveName = reader.ReadVarString();
      int score = reader.ReadInt32(littleEndian: true);

      ScoreboardIdentityType identityType = ScoreboardIdentityType.Invalid;
      long actorUniqueId = 0;
      string? customName = null;

      if (ActionType == ScoreboardActionType.Change)
      {
        identityType = (ScoreboardIdentityType)reader.ReadUInt8();
        switch (identityType)
        {
          case ScoreboardIdentityType.Player:
          case ScoreboardIdentityType.Entity:
            actorUniqueId = reader.ReadZigZong();
            break;
          case ScoreboardIdentityType.FakePlayer:
            customName = reader.ReadVarString();
            break;
        }
      }

      Entries.Add(new ScoreEntry
      {
        ScoreboardId = scoreboardId,
        ObjectiveName = objectiveName,
        Score = score,
        IdentityType = identityType,
        ActorUniqueId = actorUniqueId,
        CustomName = customName
      });
    }
  }

  public override void Serialize(Binary.BinaryWriter writer)
  {
    writer.WriteUInt8((byte)ActionType);
    writer.WriteVarUInt((uint)Entries.Count);

    for (int i = 0; i < Entries.Count; i++)
    {
      ScoreEntry entry = Entries[i];
      writer.WriteZigZong(entry.ScoreboardId);
      writer.WriteVarString(entry.ObjectiveName);
      writer.WriteInt32(entry.Score, littleEndian: true);

      if (ActionType == ScoreboardActionType.Change)
      {
        writer.WriteUInt8((byte)entry.IdentityType);
        switch (entry.IdentityType)
        {
          case ScoreboardIdentityType.Player:
          case ScoreboardIdentityType.Entity:
            writer.WriteZigZong(entry.ActorUniqueId);
            break;
          case ScoreboardIdentityType.FakePlayer:
            writer.WriteVarString(entry.CustomName ?? string.Empty);
            break;
        }
      }
    }
  }
}

public struct ScoreEntry
{
  public long ScoreboardId;
  public string ObjectiveName;
  public int Score;
  public ScoreboardIdentityType IdentityType;
  public long ActorUniqueId;
  public string? CustomName;
}
