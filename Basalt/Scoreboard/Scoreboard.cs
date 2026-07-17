using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;

namespace Basalt.Core.Scoreboard;

public sealed class Scoreboard
{
    private static long _nextEntryId;

    private readonly Player.Player _player;
    private readonly Dictionary<string, ScoreboardLine> _lines = new(StringComparer.Ordinal);
    private string _objectiveName;

    public DisplaySlotType Slot { get; }
    public string Title { get; private set; }
    public ObjectiveSortOrder SortOrder { get; }
    public bool Visible { get; private set; }

    internal Scoreboard(Player.Player player, DisplaySlotType slot, string title, ObjectiveSortOrder sortOrder)
    {
        _player = player;
        Slot = slot;
        Title = title;
        SortOrder = sortOrder;
        _objectiveName = $"basalt_{slot}_{player.RuntimeId}";
    }

    public void Show()
    {
        if (Visible)
            return;

        Visible = true;
        _player.Send(new SetDisplayObjectivePacket
        {
            DisplaySlot = Slot,
            ObjectiveName = _objectiveName,
            DisplayName = Title,
            CriteriaName = "dummy",
            SortOrder = SortOrder
        });

        if (_lines.Count > 0)
            SendAllEntries();
    }

    public void Hide()
    {
        if (!Visible)
            return;

        Visible = false;
        _player.Send(new RemoveObjectivePacket
        {
            ObjectiveName = _objectiveName
        });
    }

    public void SetTitle(string title)
    {
        Title = title;

        if (!Visible)
            return;

        _player.Send(new RemoveObjectivePacket
        {
            ObjectiveName = _objectiveName
        });

        _player.Send(new SetDisplayObjectivePacket
        {
            DisplaySlot = Slot,
            ObjectiveName = _objectiveName,
            DisplayName = title,
            CriteriaName = "dummy",
            SortOrder = SortOrder
        });

        if (_lines.Count > 0)
            SendAllEntries();
    }

    public void SetLine(string text, int score)
    {
        if (_lines.TryGetValue(text, out ScoreboardLine existing))
        {
            if (existing.Score == score)
                return;

            _lines[text] = existing with { Score = score };

            if (Visible)
                SendChangeEntry(existing.Id, text, score);

            return;
        }

        long id = Interlocked.Increment(ref _nextEntryId);
        _lines[text] = new ScoreboardLine(id, score);

        if (Visible)
            SendChangeEntry(id, text, score);
    }

    public bool RemoveLine(string text)
    {
        if (!_lines.Remove(text, out ScoreboardLine line))
            return false;

        if (Visible)
            SendRemoveEntry(line.Id);

        return true;
    }

    public void ClearLines()
    {
        if (_lines.Count == 0)
            return;

        if (Visible)
        {
            List<ScoreEntry> entries = new(_lines.Count);
            foreach ((string text, ScoreboardLine line) in _lines)
            {
                entries.Add(new ScoreEntry
                {
                    ScoreboardId = line.Id,
                    ObjectiveName = _objectiveName,
                    Score = 0,
                    IdentityType = ScoreboardIdentityType.FakePlayer,
                    ActorUniqueId = 0,
                    CustomName = null
                });
            }

            _player.Send(new SetScorePacket
            {
                ActionType = ScoreboardActionType.Remove,
                Entries = entries
            });
        }

        _lines.Clear();
    }

    private void SendChangeEntry(long id, string text, int score)
    {
        _player.Send(new SetScorePacket
        {
            ActionType = ScoreboardActionType.Change,
            Entries =
          [
            new ScoreEntry
        {
          ScoreboardId = id,
          ObjectiveName = _objectiveName,
          Score = score,
          IdentityType = ScoreboardIdentityType.FakePlayer,
          ActorUniqueId = 0,
          CustomName = text
        }
          ]
        });
    }

    private void SendRemoveEntry(long id)
    {
        _player.Send(new SetScorePacket
        {
            ActionType = ScoreboardActionType.Remove,
            Entries =
          [
            new ScoreEntry
        {
          ScoreboardId = id,
          ObjectiveName = _objectiveName,
          Score = 0,
          IdentityType = ScoreboardIdentityType.FakePlayer,
          ActorUniqueId = 0,
          CustomName = null
        }
          ]
        });
    }

    private void SendAllEntries()
    {
        List<ScoreEntry> entries = new(_lines.Count);
        foreach ((string text, ScoreboardLine line) in _lines)
        {
            entries.Add(new ScoreEntry
            {
                ScoreboardId = line.Id,
                ObjectiveName = _objectiveName,
                Score = line.Score,
                IdentityType = ScoreboardIdentityType.FakePlayer,
                ActorUniqueId = 0,
                CustomName = text
            });
        }

        _player.Send(new SetScorePacket
        {
            ActionType = ScoreboardActionType.Change,
            Entries = entries
        });
    }
}

internal readonly record struct ScoreboardLine(long Id, int Score);
