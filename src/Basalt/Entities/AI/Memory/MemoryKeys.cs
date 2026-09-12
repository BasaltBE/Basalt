namespace Basalt.Core.Entities.AI.Memory;

public static class MemoryKeys {
    public static readonly MemoryKey AttackTarget = new("attack_target");
    public static readonly MemoryKey AvoidTarget = new("avoid_target");
    public static readonly MemoryKey WalkTarget = new("walk_target");
    public static readonly MemoryKey LookTarget = new("look_target");
    public static readonly MemoryKey HurtBy = new("hurt_by");
    public static readonly MemoryKey IsPanicking = new("is_panicking");
    public static readonly MemoryKey Home = new("home");
    public static readonly MemoryKey JobSite = new("job_site");
    public static readonly MemoryKey MeetingPoint = new("meeting_point");
}
