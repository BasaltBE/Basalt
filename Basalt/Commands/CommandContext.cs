namespace Basalt.Core.Commands;

using Basalt.Protocol.Types;
using Player = Player.Player;
using EntityInstance = Entities.Entity;
using ServerInstance = Server;

/// <summary>
/// Identifies who sent the command.
/// </summary>
public abstract class CommandSender
{
    public sealed class PlayerSender(Player player) : CommandSender
    {
        public Player Player { get; } = player;
    }

    public sealed class ServerSender : CommandSender { }

    public Player? AsPlayer() => this is PlayerSender ps ? ps.Player : null;
    public bool IsPlayer => this is PlayerSender;
}

/// <summary>
/// Context passed to command handlers during execution.
/// Contains the server, sender, and parsed arguments.
/// </summary>
public sealed class CommandContext
{
    public required ServerInstance Server { get; init; }
    public required CommandSender Sender { get; init; }
    public required string Raw { get; init; }

    /// <summary>
    /// Parsed named arguments from the matched overload.
    /// </summary>
    internal List<CommandArgument> Arguments { get; init; } = [];

    /// <summary>
    /// Gets a typed argument by name.
    /// </summary>
    public T? Get<T>(string name) where T : CommandEnum
    {
        for (int i = 0; i < Arguments.Count; i++)
        {
            if (string.Equals(Arguments[i].Name, name, StringComparison.Ordinal) && Arguments[i].Value is T value)
                return value;
        }
        return null;
    }

    /// <summary>
    /// Resolves a target selector string into entities.
    /// </summary>
    public EntityInstance[] ResolveTargets(string selector)
    {
        Player? context = Sender.AsPlayer();

        if (selector == "@s")
            return context is null ? [] : [context];

        if (selector == "@a")
            return Server.Players.Values.ToArray<EntityInstance>();

        if (selector == "@e")
        {
            if (context?.Dimension is not null)
                return context.Dimension.Entities.ToArray();
            return Server.Worlds
                .SelectMany(w => w.Dimensions)
                .SelectMany(d => d.Entities)
                .ToArray();
        }

        if (selector == "@p")
        {
            Player? nearest = null;
            float nearestDist = float.MaxValue;
            foreach (Player candidate in Server.Players.Values)
            {
                if (context is not null && candidate.Dimension != context.Dimension)
                    continue;

                float dx = candidate.Location.X - (context?.Location.X ?? candidate.Location.X);
                float dy = candidate.Location.Y - (context?.Location.Y ?? candidate.Location.Y);
                float dz = candidate.Location.Z - (context?.Location.Z ?? candidate.Location.Z);
                float dist = dx * dx + dy * dy + dz * dz;
                if (dist < nearestDist)
                {
                    nearest = candidate;
                    nearestDist = dist;
                }
            }
            return nearest is null ? [] : [nearest];
        }

        if (selector == "@r")
        {
            Player[] all = Server.Players.Values.ToArray();
            if (all.Length == 0) return [];
            return [all[Random.Shared.Next(all.Length)]];
        }

        // Try by username
        foreach (Player candidate in Server.Players.Values)
        {
            if (string.Equals(candidate.Username, selector, StringComparison.OrdinalIgnoreCase))
                return [candidate];
        }

        return [];
    }

    /// <summary>
    /// Gets the player who sent the command, or returns an error if it was the server.
    /// </summary>
    public Player? RequirePlayer(out CommandResult? error)
    {
        Player? player = Sender.AsPlayer();
        if (player is null)
        {
            error = CommandResult.Error("This command must be run by a player.");
            return null;
        }
        error = null;
        return player;
    }
}

/// <summary>
/// A named argument: parameter name paired with its parsed enum value.
/// </summary>
internal sealed class CommandArgument
{
    public string Name { get; }
    public CommandEnum Value { get; }

    public CommandArgument(string name, CommandEnum value)
    {
        Name = name;
        Value = value;
    }
}
