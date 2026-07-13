namespace Basalt.Core.Commands;

/// <summary>
/// The result of a command execution.
/// </summary>
public sealed class CommandResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }

    public static readonly CommandResult Ok = new() { Success = true };
    public static readonly CommandResult Fail = new() { Success = false };

    public static CommandResult OkMessage(string message) => new() { Success = true, Message = message };
    public static CommandResult Error(string message) => new() { Success = false, Message = message };
}
