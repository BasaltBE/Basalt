namespace Basalt.Server.Commands;

/// <summary>
/// Marker for the optional dimension command parameter. Values are built dynamically from registered world dimensions.
/// </summary>
public sealed class DimensionEnum : CustomEnum
{
    public static readonly string[] Values = [];

    public DimensionEnum() : base("dimension") { }
}
