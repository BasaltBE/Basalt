namespace Basalt.Core.Entities.AI;

using Basalt.Core.Entities;
using Basalt.Core.Entities.AI.Memory;
using Basalt.Core.Entities.AI.Goal;

public sealed class AiContext(
    Entity entity,
    MemoryStore memories,
    GoalSelector goals,
    ulong currentTick) {
    public Entity Entity { get; } = entity ?? throw new ArgumentNullException(nameof(entity));
    public MemoryStore Memories { get; } = memories ?? throw new ArgumentNullException(nameof(memories));
    public GoalSelector Goals { get; } = goals ?? throw new ArgumentNullException(nameof(goals));
    public ulong CurrentTick { get; } = currentTick;
}
