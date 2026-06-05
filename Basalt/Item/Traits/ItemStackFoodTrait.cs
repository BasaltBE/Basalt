namespace Basalt.Core.Item.Traits;

using Basalt.Protocol.Nbt;
using Basalt.Core.Item.Components;


public sealed class ItemStackFoodTrait : ItemTrait
{
    public new static string Identifier => "food";
    public new static readonly Type? Component = typeof(ItemTypeFoodComponent);

    public int Nutrition;
    public float SaturationModifier;
    public bool CanAlwaysEat;
    public string UsingConvertsTo = string.Empty;

    public ItemStackFoodTrait(ItemStack itemStack) : base(itemStack)
    {
    }

    public override void OnAdd()
    {
        ItemTypeFoodComponent? food = ItemStack.Type.Components.GetComponent<ItemTypeFoodComponent>();
        if (food is null)
        {
            return;
        }

        Nutrition = food.GetNutrition();
        SaturationModifier = food.GetSaturationModifier();
        CanAlwaysEat = food.CanAlwaysEat();
        UsingConvertsTo = food.GetUsingConvertsTo();
    }

    public void SetNutrition(int value)
    {
        Nutrition = value;
    }

    public void SetSaturationModifier(float value)
    {
        SaturationModifier = value;
    }

    public void SetCanAlwaysEat(bool value)
    {
        CanAlwaysEat = value;
    }

    public void SetUsingConvertsTo(string value)
    {
        UsingConvertsTo = value;
    }

    public override void OnRead(CompoundTag tag)
    {
        Nutrition = tag.Get<IntTag>("nutrition")?.Value ?? Nutrition;
        SaturationModifier = tag.Get<FloatTag>("saturationModifier")?.Value ?? SaturationModifier;
        CanAlwaysEat = (tag.Get<ByteTag>("canAlwaysEat")?.Value ?? (CanAlwaysEat ? (sbyte)1 : (sbyte)0)) != 0;
        UsingConvertsTo = tag.Get<StringTag>("usingConvertsTo")?.Value ?? UsingConvertsTo;
    }

    public override void OnWrite(CompoundTag tag)
    {
        tag.Set("nutrition", new IntTag { Value = Nutrition });
        tag.Set("saturationModifier", new FloatTag { Value = SaturationModifier });
        tag.Set("canAlwaysEat", new ByteTag { Value = CanAlwaysEat ? (sbyte)1 : (sbyte)0 });
        tag.Set("usingConvertsTo", new StringTag { Value = UsingConvertsTo });
    }
}
