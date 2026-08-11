using BedrockProtocol.Enums;
using BedrockProtocol.Packets;
using BedrockProtocol.Types;

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

            if (Visible) {
                SendChangeEntry(updated.Id, text, updated.Score);
            }

            return;
        }

        long id = Interlocked.Increment(ref _nextEntryId);

        _lines[text] = new ScoreboardLine(
            Id: id,
            Score: score
        );

        if (Visible) {
            SendChangeEntry(id, text, score);
        }
    }

    public bool RemoveLine(string text) {
        if (!_lines.Remove(text, out ScoreboardLine line)) {
            return false;
        }

        if (Visible) {
            SendRemoveEntry(line.Id);
        }

        return true;
    }

    public void ClearLines() {
        if (_lines.Count == 0) {
            return;
        }

        if (Visible) {
            List<SetScoreScoreInfoVariant> entries = new(_lines.Count);

            foreach ((_, ScoreboardLine line) in _lines) {
                entries.Add(CreateRemoveEntry(line.Id));
            }

            _player.Send(new SetScorePacket {
                ScoreInfo = entries
            });
        }

        _lines.Clear();
    }

    private void SendChangeEntry(long id, string text, int score) {
        _player.Send(new SetScorePacket {
            ScoreInfo = [
                new ChangeFakePlayerScore {
                    Action = ScorePacketEntryAction.ChangeFakePlayer,
                    FakePlayerName = text,
                    ObjectiveName = _objectiveName,
                    ScoreValue = score,
                    ScoreboardId = new ScoreboardId {
                        Value = id
                    }
                }
            ]
        });
    }

    private void SendRemoveEntry(long id) {
        _player.Send(new SetScorePacket {
            ScoreInfo = [
                CreateRemoveEntry(id)
            ]
        });
    }

    private RemoveScore CreateRemoveEntry(long id) {
        return new RemoveScore {
            Action = ScorePacketEntryAction.Remove,
            ObjectiveName = _objectiveName,
            ScoreboardId = new ScoreboardId {
                Value = id
            }
        };
    }

    private void SendAllEntries() {
        List<SetScoreScoreInfoVariant> entries = new(_lines.Count);

        foreach ((string text, ScoreboardLine line) in _lines) {
            entries.Add(new ChangeFakePlayerScore {
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
            ScoreInfo = entries
        });
    }
}

internal readonly record struct ScoreboardLine(
    long Id,
    int Score
);