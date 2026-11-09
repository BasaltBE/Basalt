namespace BasaltBE.Basalt.Entities.Traits;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EntityTraitAppliesToAttribute : Attribute {
    public string[] Identifiers = [];
    public string[] Families = [];
    public string[] Components = [];
}
