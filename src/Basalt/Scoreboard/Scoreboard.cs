using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

namespace Basalt.Core.Scoreboard;

public enum DisplaySlotType {
    List,
    Sidebar,
    BelowName
}

public static class DisplaySlotTypeExtensions {
    public static string ToProtoString(this DisplaySlotType value) {
        return value switch {
            DisplaySlotType.List => "list",
            DisplaySlotType.Sidebar => "sidebar",
            DisplaySlotType.BelowName => "belowname",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }
}

public sealed class Scoreboard {
    private static long _nextEntryId;

    private readonly Player.Player _player;
    private readonly Dictionary<string, ScoreboardLine> _lines = new(StringComparer.Ordinal);
    private Dictionary<string, ScoreboardLine>? _updatingLines;
    private readonly string _objectiveName;

    public DisplaySlotType Slot { get; }
    public string Title { get; private set; }
    public ObjectiveSortOrder SortOrder { get; }
    public bool Visible { get; private set; }

    internal Scoreboard(
        Player.Player player,
        DisplaySlotType slot,
        string title,
        ObjectiveSortOrder sortOrder
    ) {
        _player = player;
        Slot = slot;
        Title = title;
        SortOrder = sortOrder;
        _objectiveName = $"basalt_{slot.ToProtoString()}_{player.RuntimeId}";
    }

    public void Show() {
        if (Visible) {
            return;
        }

        Visible = true;

        _player.Send(new SetDisplayObjectivePacket {
            DisplaySlotName = Slot.ToProtoString(),
            ObjectiveName = _objectiveName,
            ObjectiveDisplayName = Title,
            CriteriaName = "dummy",
            SortOrder = (int)SortOrder
        });

        if (_lines.Count > 0) {
            SendAllEntries();
        }
    }

    public void Hide() {
        if (!Visible) {
            return;
        }

        Visible = false;

        _player.Send(new RemoveObjectivePacket {
            ObjectiveName = _objectiveName
        });
    }

    public void SetTitle(string title) {
        if (Title == title) {
            return;
        }

        Title = title;

        if (!Visible) {
            return;
        }

        _player.Send(new SetDisplayObjectivePacket {
            DisplaySlotName = Slot.ToProtoString(),
            ObjectiveName = _objectiveName,
            ObjectiveDisplayName = Title,
            CriteriaName = "dummy",
            SortOrder = (int)SortOrder
        });
    }

    public void SetLine(string text, int score) {
        ArgumentException.ThrowIfNullOrEmpty(text);

        if (_lines.TryGetValue(text, out ScoreboardLine existing)) {
            if (existing.Score == score) {
                return;
            }

            ScoreboardLine updated = existing with {
                Score = score
            };

            _lines[text] = updated;

            if (Visible && _updatingLines is null) {
                SendChangeEntry(updated.Id, text, updated.Score);
            }

            return;
        }

        long id = Interlocked.Increment(ref _nextEntryId);

        _lines[text] = new ScoreboardLine(
            Id: id,
            Score: score
        );

        if (Visible && _updatingLines is null) {
            SendChangeEntry(id, text, score);
        }
    }

    public bool RemoveLine(string text) {
        if (!_lines.Remove(text, out ScoreboardLine line)) {
            return false;
        }

        if (Visible && _updatingLines is null) {
            SendRemoveEntry(line.Id);
        }

        return true;
    }

    public void BeginUpdate() {
        _updatingLines = new Dictionary<string, ScoreboardLine>(_lines, StringComparer.Ordinal);
    }

    public void EndUpdate() {
        if (_updatingLines is null)
            return;

        Dictionary<string, ScoreboardLine> previous = _updatingLines;
        _updatingLines = null;

        if (!Visible)
            return;

        List<ScoreEntry> entries = new(previous.Count + _lines.Count);
        foreach ((string text, ScoreboardLine line) in previous) {
            if (!_lines.TryGetValue(text, out ScoreboardLine current) || current.Id != line.Id)
                entries.Add(CreateRemoveEntry(line.Id));
        }

        foreach ((string text, ScoreboardLine line) in _lines) {
            if (!previous.TryGetValue(text, out ScoreboardLine old) ||
                old.Id != line.Id || old.Score != line.Score)
                entries.Add(CreateChangeEntry(line.Id, text, line.Score));
        }

        if (entries.Count > 0)
            _player.Send(new SetScorePacket { Entries = [.. entries] });
    }

    public void ClearLines() {
        if (_lines.Count == 0) {
            return;
        }

        if (Visible && _updatingLines is null) {
            List<ScoreEntry> entries = new(_lines.Count);

            foreach ((_, ScoreboardLine line) in _lines) {
                entries.Add(CreateRemoveEntry(line.Id));
            }

            _player.Send(new SetScorePacket {
                Entries = [.. entries]
            });
        }

        _lines.Clear();
    }

    private void SendChangeEntry(long id, string text, int score) {
        _player.Send(new SetScorePacket {
            Entries = [CreateChangeEntry(id, text, score)]
        });
    }

    private ScoreEntry CreateChangeEntry(long id, string text, int score) {
        return new ScoreEntry {
            Action = ScorePacketEntryAction.ChangeFakePlayer,
            FakePlayerName = text,
            ObjectiveName = _objectiveName,
            ScoreValue = score,
            ScoreboardId = new ScoreboardId { Value = id }
        };
    }

    private void SendRemoveEntry(long id) {
        _player.Send(new SetScorePacket {
            Entries = [
                CreateRemoveEntry(id)
            ]
        });
    }

    private ScoreEntry CreateRemoveEntry(long id) {
        return new ScoreEntry {
            Action = ScorePacketEntryAction.Remove,
            ObjectiveName = _objectiveName,
            ScoreboardId = new ScoreboardId {
                Value = id
            }
        };
    }

    private void SendAllEntries() {
        List<ScoreEntry> entries = new(_lines.Count);

        foreach ((string text, ScoreboardLine line) in _lines) {
            entries.Add(new ScoreEntry {
                Action = ScorePacketEntryAction.ChangeFakePlayer,
                FakePlayerName = text,
                ObjectiveName = _objectiveName,
                ScoreValue = line.Score,
                ScoreboardId = new ScoreboardId {
                    Value = line.Id
                }
            });
        }

        _player.Send(new SetScorePacket {
            Entries = [.. entries]
        });
    }
}

internal readonly record struct ScoreboardLine(
    long Id,
    int Score
);
