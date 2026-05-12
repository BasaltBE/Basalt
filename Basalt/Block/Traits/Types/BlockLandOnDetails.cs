using Basalt.Core;
using Basalt.Entity;
using Basalt.Protocol.Types;

namespace Basalt.Block.Traits.Types;

public readonly record struct BlockLandOnDetails(Basalt.Entity.Entity Entity, BlockPos BlockPosition);
