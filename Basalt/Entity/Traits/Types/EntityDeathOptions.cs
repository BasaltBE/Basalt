using Basalt.Protocol.Enums;

namespace Basalt.Entity.Traits.Types;

public readonly record struct EntityDeathOptions(
    bool Cancel = false,
    global::Basalt.Entity.Entity? KillerSource = null,
    ActorDamageCause? DamageCause = null
);
