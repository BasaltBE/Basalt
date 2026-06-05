namespace Basalt.Core.Commands;

using EntityInstance = Entities.Entity;
using Player = Player.Player;

public class TargetEnum : CommandEnum
{
    public string Raw = string.Empty;

    public TargetEnum() : base("target") { }

    public TargetEnum(string raw, EntityInstance[] entities, string[]? offlineUsernames = null) : base("target")
    {
        Raw = raw;
        Entities = entities;
        OfflineUsernames = offlineUsernames ?? [];
    }

    public EntityInstance[] Entities = [];
    public string[] OfflineUsernames = [];

    public override bool Parse(CommandExecutionState state, CommandParameter parameter, string[] tokens, ref int tokenIndex)
    {
        if (tokenIndex >= tokens.Length)
        {
            return false;
        }

        Raw = tokens[tokenIndex];
        Player? player = state.Executor is PlayerExecutor executor ? executor.Player : null;
        Entities = ResolveTargets(state.Server, player, Raw);
        OfflineUsernames = ResolveOfflineTargets(state.Server, Raw, Entities);
        tokenIndex++;
        return true;
    }

    public static EntityInstance[] ResolveTargets(Server server, Player? player, string token)
    {
        if (token == "@s")
        {
            return player is null ? [] : [player];
        }

        if (token == "@a")
        {
            return server.Players.Values.ToArray<EntityInstance>();
        }

        if (token == "@e")
        {
            if (player is not null)
            {
                return player.Dimension?.Entities.ToArray() ?? [];
            }

            return server.Worlds.SelectMany(world => world.Dimensions).SelectMany(dimension => dimension.Entities).ToArray();
        }

        if (token == "@p")
        {
            Player? nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (Player candidate in server.Players.Values)
            {
                if (player is not null && candidate.Dimension != player.Dimension)
                {
                    continue;
                }

                float dx = candidate.Location.X - (player?.Location.X ?? candidate.Location.X);
                float dy = candidate.Location.Y - (player?.Location.Y ?? candidate.Location.Y);
                float dz = candidate.Location.Z - (player?.Location.Z ?? candidate.Location.Z);
                float distance = dx * dx + dy * dy + dz * dz;
                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistance = distance;
            }

            return nearest is null ? [] : [nearest];
        }

        foreach (Player candidate in server.Players.Values)
        {
            if (string.Equals(candidate.Username, token, StringComparison.OrdinalIgnoreCase))
            {
                return [candidate];
            }
        }

        return [];
    }

    public static string[] ResolveOfflineTargets(Server server, string token, EntityInstance[] onlineTargets)
    {
        if (onlineTargets.Length > 0 || token.StartsWith('@'))
        {
            return [];
        }

        return [];
    }

    public static List<Player> ResolvePlayers(Server server, Player? context, string token)
    {
        List<Player> players = [];
        EntityInstance[] entities = ResolveTargets(server, context, token);
        for (int i = 0; i < entities.Length; i++)
        {
            if (entities[i] is Player player)
            {
                players.Add(player);
            }
        }

        return players;
    }

    public static CommandResult? ResolveSinglePlayerTarget(
        Server server,
        Player? context,
        string token,
        string emptyMessage,
        string ambiguousMessage)
    {
        if (token == "@a")
        {
            return CommandResult.Message(ambiguousMessage, false);
        }

        List<Player> players = ResolvePlayers(server, context, token);
        if (players.Count == 0)
        {
            return CommandResult.Message(emptyMessage, false);
        }

        if (players.Count > 1)
        {
            return CommandResult.Message(ambiguousMessage, false);
        }

        return null;
    }
}







