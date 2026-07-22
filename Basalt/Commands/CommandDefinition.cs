namespace Basalt.Core.Commands;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Defines a single parameter in a command overload.
/// </summary>
public sealed class ParameterDefinition {
    public required string Name { get; init; }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public required Type Type { get; init; }

    public bool Optional { get; init; }
}

/// <summary>
/// Defines one overload (signature) of a command.
/// </summary>
public sealed class OverloadDefinition {
    public ParameterDefinition[] Parameters { get; init; } = [];
}

/// <summary>
/// Command definities for mc protocol
/// </summary>
public sealed class CommandDefinition {
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string[] Aliases { get; init; } = [];
    public string[] Permissions { get; init; } = [];
    public required OverloadDefinition[] Overloads { get; init; }
    public required CommandHandler Handler { get; init; }
}
