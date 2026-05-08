using System.Reflection;

namespace Basalt.Item.Traits;

public abstract class ItemTrait
{
    public static readonly string[] Types = [];
    public static readonly string[] Tags = [];
    public static readonly Type? Component = null;
    public static readonly Type[] Components = [];

    protected ItemStack ItemStack { get; }
    public virtual string Identifier
    {
        get
        {
            if (GetType().GetProperty("Identifier", BindingFlags.Public | BindingFlags.Static) is PropertyInfo property &&
                property.PropertyType == typeof(string) &&
                property.GetValue(null) is string identifier &&
                !string.IsNullOrWhiteSpace(identifier))
            {
                return identifier;
            }

            return GetType().FullName ?? GetType().Name;
        }
    }

    protected ItemTrait(ItemStack itemStack)
    {
        ItemStack = itemStack;
    }

    public virtual void OnAdd()
    {
    }

    public virtual void OnRemove()
    {
    }
}
