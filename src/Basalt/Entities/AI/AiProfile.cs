namespace Basalt.Core.Entities.AI;

using Basalt.Core.Entities.AI.Goal;
using GoalType = Basalt.Core.Entities.AI.Goal.Goal;

public sealed class AiProfile(
    string identifier,
    IReadOnlyList<Func<Entity, GoalType>> goalFactories) {
    public string Identifier { get; } = identifier ?? throw new ArgumentNullException(nameof(identifier));
    public IReadOnlyList<Func<Entity, GoalType>> GoalFactories { get; } = goalFactories ?? throw new ArgumentNullException(nameof(goalFactories));
}
