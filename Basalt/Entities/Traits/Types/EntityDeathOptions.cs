namespace Basalt.Core.Entities.Traits.Types;

using Basalt.Protocol.Enums;

public readonly record struct EntityDeathOptions(
    bool Cancel = false,
    global::Basalt.Core.Entities.Entity? KillerSource = null,
    ActorDamageCause? DamageCause = null
);






