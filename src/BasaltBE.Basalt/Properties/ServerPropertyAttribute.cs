namespace BasaltBE.Basalt.Properties;

[AttributeUsage(AttributeTargets.Field)]
public sealed class ServerPropertyAttribute(
    string category,
    int order,
    string key,
    string comment
) : Attribute {
    public readonly string Category = category;
    public readonly int Order = order;
    public readonly string Key = key;
    public readonly string Comment = comment;
}
