namespace Basalt.Core.Entity.Traits.Types;
using Basalt.Protocol.Enums;

public readonly record struct EntityDeathOptions(
    bool Cancel = false,
    global::Basalt.Core.Entity.Entity? KillerSource = null,
    ActorDamageCause? DamageCause = null
);






