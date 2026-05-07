namespace Basalt.Item.Traits;

public abstract class ItemTrait
{
    protected ItemStack ItemStack { get; }

    protected ItemTrait(ItemStack itemStack)
    {
        ItemStack = itemStack;
    }
}
