namespace Basalt.Commands;

public abstract class CustomEnum : CommandEnum
{
    public string? Value;

    protected CustomEnum(string identifier) : base(identifier)
    {
        Options = GetValues(GetType());
    }

    static string[] GetValues(Type type)
    {
        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.FlattenHierarchy;

        if (type.GetField("Values", flags)?.GetValue(null) is string[] fieldValues)
        {
            return fieldValues;
        }

        if (type.GetProperty("Values", flags)?.GetValue(null) is string[] propertyValues)
        {
            return propertyValues;
        }

        throw new InvalidOperationException($"Command enum '{type.FullName}' must define static string[] Values.");
    }
}
