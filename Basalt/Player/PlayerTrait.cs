namespace Basalt.Core.Player;


using Basalt.Core.Entity.Traits;
using Entity = Basalt.Core.Entity.Entity;

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






