namespace Basalt.Core.Entities.AI.Goal;

[Flags]
public enum GoalControl : byte {
    None = 0,
    Move = 1,
    Look = 2,
    Jump = 4,
    Target = 8,
    Interact = 16,
    Attack = 32
}
