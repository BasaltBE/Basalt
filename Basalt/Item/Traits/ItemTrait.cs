namespace Basalt.Core.Item.Traits;

using System.Reflection;
using BedrockProtocol.Nbt;
using Basalt.Core.Item.Traits.Types;


public abstract class ItemTrait {
    public static readonly string[] Types = [];
    public static readonly string[] Tags = [];
    public static readonly Type? Component;
    public static readonly Type[] Components = [];

    protected ItemStack ItemStack { get; }
    public virtual string Identifier {
        get {
#pragma warning disable IL2075
            if (GetType().GetProperty("Identifier", BindingFlags.Public | BindingFlags.Static) is PropertyInfo property &&
                property.PropertyType == typeof(string) &&
                property.GetValue(null) is string identifier &&
                !string.IsNullOrWhiteSpace(identifier)) {
                return identifier;
            }

            return GetType().FullName ?? GetType().Name;
#pragma warning restore IL2075
        }
    }

    protected ItemTrait(ItemStack itemStack) {
        ItemStack = itemStack;
    }

    public virtual void OnAdd() {
    }

    public virtual void OnRemove() {
    }

    public virtual void OnRead(CompoundTag tag) {
    }

    public virtual void OnWrite(CompoundTag tag) {
    }

    public virtual void OnUseOnAir(ItemUseOnAirDetails details) {
    }

    public virtual void OnUseOnBlock(ItemUseOnBlockDetails details) {
    }

    public virtual void OnPlace(ItemPlaceDetails details) {
    }

    public virtual void OnUseOnEntity(ItemUseOnEntityDetails details) {
    }

    public virtual void OnUseAttack(ItemUseAttackDetails details) {
    }

    public virtual void OnBreakBlock(ItemBreakBlockDetails details) {
    }
}






