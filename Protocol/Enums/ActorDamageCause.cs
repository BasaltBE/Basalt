namespace Basalt.Protocol.Enums;

public enum ActorDamageCause : int
{
    None = -1,
    Override = 0,
    Contact = 1,
    EntityAttack = 2,
    Projectile = 3,
    Suffocation = 4,
    Fall = 5,
    Fire = 6,
    FireTick = 7,
    Lava = 8,
    Drowning = 9,
    BlockExplosion = 10,
    EntityExplosion = 11,
    Void = 12,
    Suicide = 13,
    Magic = 14,
    Wither = 15,
    Starve = 16
}
