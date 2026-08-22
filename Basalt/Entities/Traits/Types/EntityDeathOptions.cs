using Basalt.BedrockProtocol.Enums;

namespace Basalt.Core.Entities.Traits.Types;


public readonly record struct EntityDeathOptions(
    bool Cancel = false,
    global::Basalt.Core.Entities.Entity? KillerSource = null,
    ActorDamageCause? DamageCause = null
);






