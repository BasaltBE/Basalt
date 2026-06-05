namespace Basalt.Protocol.Enums;

/// <summary>
/// Controls how command output is delivered.
/// </summary>
public enum CommandOutputType : byte
{
    /// <summary>
    /// No command output is sent.
    /// </summary>
    None,
    /// <summary>
    /// Only the last command output is sent.
    /// </summary>
    LastOutput,
    /// <summary>
    /// Command output is suppressed.
    /// </summary>
    Silent,
    /// <summary>
    /// All command output is sent.
    /// </summary>
    AllOutput,
    /// <summary>
    /// Command output contains a data set.
    /// </summary>
    DataSet
}
