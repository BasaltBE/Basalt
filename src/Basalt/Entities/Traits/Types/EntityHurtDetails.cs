namespace Basalt.Core.Entities.Traits.Types;

using Basalt.BedrockProtocol.Enums;

public readonly record struct EntityHurtDetails(ActorDamageCause? Cause, Entity? Damager);
