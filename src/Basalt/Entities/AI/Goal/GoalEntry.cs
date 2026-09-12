namespace Basalt.Core.Entities.AI.Goal;

public sealed class GoalEntry(Goal goal) {
    public Goal Goal { get; } = goal ?? throw new ArgumentNullException(nameof(goal));
    public ulong NextUpdateTick { get; set; }
}
