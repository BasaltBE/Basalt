namespace Basalt.Core.Item.Traits;

using Basalt.BedrockProtocol.NBT;
using Basalt.Core.Item.Components;


public sealed class ItemStackFoodTrait : ItemTrait {
    public new static string Identifier => "food";
    public new static readonly Type? Component = typeof(ItemTypeFoodComponent);

    public int Nutrition;
    public float SaturationModifier;
    public bool CanAlwaysEat;
    public string UsingConvertsTo = string.Empty;

    public ItemStackFoodTrait(ItemStack itemStack) : base(itemStack) {
    }

    public override void OnAdd() {
        ItemTypeFoodComponent? food = ItemStack.Type.Components.GetComponent<ItemTypeFoodComponent>();
        if (food is null) {
            return;
        }

        Nutrition = food.GetNutrition();
        SaturationModifier = food.GetSaturationModifier();
        CanAlwaysEat = food.CanAlwaysEat();
        UsingConvertsTo = food.GetUsingConvertsTo();
    }

    public void SetNutrition(int value) {
        Nutrition = value;
    }

    public void SetSaturationModifier(float value) {
        SaturationModifier = value;
    }

    public void SetCanAlwaysEat(bool value) {
        CanAlwaysEat = value;
    }

    public void SetUsingConvertsTo(string value) {
        UsingConvertsTo = value;
    }

    public override void OnRead(CompoundTag tag) {
        ItemTypeFoodComponent? food = ItemStack.Type.Components.GetComponent<ItemTypeFoodComponent>();
        int componentNutrition = food?.GetNutrition() ?? Nutrition;
        float componentSaturationModifier = food?.GetSaturationModifier() ?? SaturationModifier;
        IntTag? nutritionTag = tag.Get<IntTag>("nutrition");
        FloatTag? saturationModifierTag = tag.Get<FloatTag>("saturationModifier");
        Nutrition = nutritionTag is not null && nutritionTag.Value > 0
            ? nutritionTag.Value
            : componentNutrition;
        SaturationModifier = saturationModifierTag is not null && saturationModifierTag.Value > 0f
            ? saturationModifierTag.Value
            : componentSaturationModifier;
        CanAlwaysEat = (tag.Get<ByteTag>("canAlwaysEat")?.Value ?? (CanAlwaysEat ? (sbyte)1 : (sbyte)0)) != 0;
        UsingConvertsTo = tag.Get<StringTag>("usingConvertsTo")?.Value ?? UsingConvertsTo;
    }

    public override void OnWrite(CompoundTag tag) {
        tag.Set("nutrition", new IntTag { Value = Nutrition });
        tag.Set("saturationModifier", new FloatTag { Value = SaturationModifier });
        tag.Set("canAlwaysEat", new ByteTag { Value = CanAlwaysEat ? (sbyte)1 : (sbyte)0 });
        tag.Set("usingConvertsTo", new StringTag { Value = UsingConvertsTo });
    }
}
