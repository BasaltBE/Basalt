using Basalt.Core;

namespace Basalt.Entity.Traits.PlayerTraits;

public abstract class PlayerTrait : EntityTrait
{
    protected Player Player { get; }

    protected PlayerTrait(Entity entity) : base(entity)
    {
        if (entity is not Player player)
        {
            throw new ArgumentException("PlayerTrait requires a Player entity.", nameof(entity));
        }

        Player = player;
    }
}
