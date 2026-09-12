namespace Basalt.Tests;

using Basalt.Core.Entities;
using Basalt.Core.Entities.AI;
using Basalt.Core.Entities.AI.Goal;
using Basalt.Core.Entities.AI.Memory;

public sealed class GoalSelectorTests {
    [Fact]
    public void HigherPriorityGoalReplacesLowerPriorityGoal() {
        Entity entity = new(EntityIdentifier.Villager.ToIdentifierString());
        MemoryStore memories = new();
        GoalSelector selector = new();
        AiContext context = new(entity, memories, selector, 1);
        TestGoal low = new(10, GoalControl.Move, true, true);
        TestGoal high = new(1, GoalControl.Move, true, false);
        selector.Add(low);
        selector.Add(high);

        selector.Tick(context);
        high.Allowed = true;
        selector.Tick(new AiContext(entity, memories, selector, 2));

        Assert.False(low.Running);
        Assert.True(high.Running);
        Assert.Equal(1, low.Stops);
    }

    [Fact]
    public void ConflictingGoalsDoNotRunTogether() {
        Entity entity = new(EntityIdentifier.Villager.ToIdentifierString());
        MemoryStore memories = new();
        GoalSelector selector = new();
        TestGoal first = new(1, GoalControl.Move, true, true);
        TestGoal second = new(2, GoalControl.Move, true, true);
        selector.Add(first);
        selector.Add(second);

        selector.Tick(new AiContext(entity, memories, selector, 1));

        Assert.True(first.Running);
        Assert.False(second.Running);
    }

    private sealed class TestGoal(int priority, GoalControl controls, bool interruptible, bool allowed)
        : Goal(priority, controls, interruptible) {
        public bool Allowed { get; set; } = allowed;
        public int Stops { get; private set; }

        public override bool CanUse(AiContext context) => Allowed;

        public override bool CanContinue(AiContext context) => Allowed;

        public override void Stop(AiContext context) {
            Stops++;
        }
    }
}
