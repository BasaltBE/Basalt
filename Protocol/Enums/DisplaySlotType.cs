namespace Basalt.Protocol.Enums;

/// <summary>
/// Where the scoreboard objective is displayed on the client.
/// Bedrock sends these as strings in the SetDisplayObjective packet.
/// </summary>
public enum DisplaySlotType {
    List,
    Sidebar,
    BelowName
}

public static class DisplaySlotTypeExtensions {
    private const string ListString = "list";
    private const string SidebarString = "sidebar";
    private const string BelowNameString = "belowname";

    public static string ToProtocolString(this DisplaySlotType slot) => slot switch {
        DisplaySlotType.List => ListString,
        DisplaySlotType.Sidebar => SidebarString,
        DisplaySlotType.BelowName => BelowNameString,
        _ => SidebarString
    };

    public static DisplaySlotType FromProtocolString(string value) => value switch {
        ListString => DisplaySlotType.List,
        SidebarString => DisplaySlotType.Sidebar,
        BelowNameString => DisplaySlotType.BelowName,
        _ => DisplaySlotType.Sidebar
    };
}
