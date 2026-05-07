using Basalt.Entity;

namespace Basalt.Item.Traits;

public sealed class ItemStackDurabilityTrait : ItemTrait
{
    public ItemStackDurabilityTrait(ItemStack itemStack) : base(itemStack)
    {
    }

    public void ProcessDamage(global::Basalt.Entity.Entity _entity)
    {
    }
}
