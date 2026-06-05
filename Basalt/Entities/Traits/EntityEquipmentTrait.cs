namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Item;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;

public sealed class EntityEquipmentTrait : EntityTrait
{
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];
    public new static readonly string[] Components = ["minecraft:equipment"];

    // TODO Once container is implemented
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

    public override void OnRead(CompoundTag tag)
    {
        ListTag? armorTag = tag.Get<ListTag>("armor");
        if (armorTag is null)
        {
            return;
        }

        for (int i = 0; i < Armor.Count; i++)
        {
            Armor[i] = null;
        }

        for (int i = 0; i < armorTag.Values.Count && i < Armor.Count; i++)
        {
            if (armorTag.Values[i] is not CompoundTag itemTag)
            {
                continue;
            }

            Armor[i] = ItemStack.Deserialize(itemTag);
        }
    }

    public override void OnWrite(CompoundTag tag)
    {
        ListTag armorTag = new() { Name = "armor" };
        for (int i = 0; i < Armor.Count; i++)
        {
            ItemStack? armor = Armor[i];
            armorTag.Values.Add(armor is null ? new CompoundTag() : armor.Serialize());
        }

        tag.Set("armor", armorTag);
    }
}






