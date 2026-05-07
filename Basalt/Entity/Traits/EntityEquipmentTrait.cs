using Basalt.Item;
using Basalt.Protocol.Enums;

namespace Basalt.Entity.Traits;

public sealed class EntityEquipmentTrait : EntityTrait
{
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];
    public new static readonly string[] Components = ["minecraft:equipment"];

    public List<ItemStack?> Armor { get; } = [null, null, null, null];

    public EntityEquipmentTrait(Entity entity) : base(entity)
    {
    }

    public override EntityTrait Clone(Entity entity)
    {
        EntityEquipmentTrait clone = new(entity);
        for (int i = 0; i < Armor.Count; i++)
        {
            clone.Armor[i] = Armor[i];
        }

        return clone;
    }
}
