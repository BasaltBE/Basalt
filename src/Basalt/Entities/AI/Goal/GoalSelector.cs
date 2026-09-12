namespace Basalt.Core.Entities.AI.Goal;

using Basalt.Core.Entities.AI;

public sealed class GoalSelector {
    private readonly List<GoalEntry> _entries = [];
    private GoalControl _claimedControls;

    public IReadOnlyList<GoalEntry> Entries => _entries;

    public void Add(Goal goal) {
        ArgumentNullException.ThrowIfNull(goal);
        _entries.Add(new GoalEntry(goal));
        _entries.Sort(static (first, second) => first.Goal.Priority.CompareTo(second.Goal.Priority));
    }

    public void Tick(AiContext context) {
        ArgumentNullException.ThrowIfNull(context);
        StopInvalidGoals(context);
        StartAvailableGoals(context);
        TickRunningGoals(context);
    }

    private void StopInvalidGoals(AiContext context) {
        _claimedControls = GoalControl.None;
        for (int i = 0; i < _entries.Count; i++) {
            GoalEntry entry = _entries[i];
            Goal goal = entry.Goal;
            if (!goal.Running) {
                continue;
            }

            if (goal.CanContinue(context)) {
                _claimedControls |= goal.Controls;
                continue;
            }

            goal.Stop(context);
            goal.Running = false;
        }
    }

    private void StartAvailableGoals(AiContext context) {
        for (int i = 0; i < _entries.Count; i++) {
            GoalEntry entry = _entries[i];
            Goal goal = entry.Goal;
            if (goal.Running || !goal.CanUse(context)) {
                continue;
            }

            if ((_claimedControls & goal.Controls) != GoalControl.None) {
                StopLowerGoals(context, i, goal.Controls);
                if ((_claimedControls & goal.Controls) != GoalControl.None) {
                    continue;
                }
            }

            bool blocked = false;
            for (int j = 0; j < i; j++) {
                Goal other = _entries[j].Goal;
                if (other.Running && !other.Interruptible &&
                    (other.Controls & goal.Controls) != GoalControl.None) {
                    blocked = true;
                    break;
                }
            }

            if (blocked) {
                continue;
            }

            goal.Start(context);
            goal.Running = true;
            _claimedControls |= goal.Controls;
        }
    }

    private void StopLowerGoals(AiContext context, int index, GoalControl controls) {
        for (int i = index + 1; i < _entries.Count; i++) {
            Goal lower = _entries[i].Goal;
            if (lower.Running && lower.Interruptible &&
                (lower.Controls & controls) != GoalControl.None) {
                lower.Stop(context);
                lower.Running = false;
            }
        }

        _claimedControls = GoalControl.None;
        for (int i = 0; i < _entries.Count; i++) {
            if (_entries[i].Goal.Running) {
                _claimedControls |= _entries[i].Goal.Controls;
            }
        }
    }

    private void TickRunningGoals(AiContext context) {
        for (int i = 0; i < _entries.Count; i++) {
            GoalEntry entry = _entries[i];
            Goal goal = entry.Goal;
            if (!goal.Running || context.CurrentTick < entry.NextUpdateTick) {
                continue;
            }

            goal.Tick(context);
            entry.NextUpdateTick = context.CurrentTick + goal.UpdateInterval;
        }
    }
}
