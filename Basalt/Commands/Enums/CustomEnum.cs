namespace Basalt.Server.Commands;

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

    public override bool Parse(CommandExecutionState state, CommandParameter parameter, string[] tokens, ref int tokenIndex)
    {
        if (tokenIndex >= tokens.Length)
        {
            return false;
        }

        string token = tokens[tokenIndex];
        string? value = Options.FirstOrDefault(option => string.Equals(option, token, StringComparison.OrdinalIgnoreCase));
        if (value is null)
        {
            throw new InvalidOperationException($"Invalid value '{token}' for command parameter '{parameter.Name}'.");
        }

        Value = value;
        tokenIndex++;
        return true;
    }
}







