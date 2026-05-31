namespace Basalt.Server.Entity.Traits.Types;
using Basalt.Protocol.Enums;

public readonly record struct EntityDeathOptions(
    bool Cancel = false,
    global::Basalt.Server.Entity.Entity? KillerSource = null,
    ActorDamageCause? DamageCause = null
);






