namespace Basalt.Core.World;

/// <summary>
/// This will be used for measuring tick times and current ticks.
/// </summary>
public interface Tickable
{
    /// <summary>
    /// The current tick value
    /// </summary>
    ulong TickValue { get; set; }
    
    /// <summary>
    /// The amount of milliseconds the last tick took.
    /// </summary>
    double TickWork { get; set; }
}







