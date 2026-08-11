using BedrockProtocol.Types;

namespace Basalt.Core.Entities.Traits.Types;


public readonly record struct EntityTeleportOptions(Vec3 From, Vec3 To, bool ChangedDimension = false);






